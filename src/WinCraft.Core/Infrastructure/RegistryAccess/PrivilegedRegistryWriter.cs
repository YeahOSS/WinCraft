using System;
using System.Collections.Generic;
using Microsoft.Win32;
using WinCraft.Infrastructure.Ipc;
using WinCraft.Infrastructure.Security;

namespace WinCraft.Infrastructure.RegistryAccess
{
    /// <summary>
    /// Routes registry writes to either the current user context or the privileged host.
    /// </summary>
    internal sealed class PrivilegedRegistryWriter
    {
        internal delegate PrivilegeExecutionResult RegistryOperationAttempt(
            RegistryValueWriteRequest request,
            string operationName,
            PrivilegeLevel privilegeLevel);

        private static readonly PrivilegeLevel[] CurrentOnlyLevels =
        {
            PrivilegeLevel.Standard
        };

        private static readonly PrivilegeLevel[] AutoWithoutTrustedInstallerLevels =
        {
            PrivilegeLevel.Standard,
            PrivilegeLevel.Administrator,
            PrivilegeLevel.System
        };

        private static readonly PrivilegeLevel[] AutoLevels =
        {
            PrivilegeLevel.Standard,
            PrivilegeLevel.Administrator,
            PrivilegeLevel.System,
            PrivilegeLevel.TrustedInstaller
        };

        private readonly IPrivilegeBroker _privilegeBroker;
        private readonly RegistryOperationAttempt _operationAttempt;

        public PrivilegedRegistryWriter(IPrivilegeBroker privilegeBroker)
            : this(privilegeBroker, null)
        {
        }

        internal PrivilegedRegistryWriter(
            IPrivilegeBroker privilegeBroker,
            RegistryOperationAttempt operationAttempt)
        {
            _privilegeBroker = privilegeBroker;
            _operationAttempt = operationAttempt ?? ExecuteAttempt;
        }

        public PrivilegeExecutionResult WriteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            string valueData)
        {
            return WriteString(
                location,
                subKeyPath,
                valueName,
                valueData,
                RegistryPrivilegePolicy.Auto);
        }

        public PrivilegeExecutionResult WriteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            string valueData,
            RegistryPrivilegePolicy privilegePolicy)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName,
                ValueData = valueData,
                ValueKind = RegistryValueKind.String
            };

            return ExecuteWithPolicy(request, ElevatedOperations.RegistryWrite, privilegePolicy);
        }

        public PrivilegeExecutionResult WriteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            string valueData,
            PrivilegeLevel privilegeLevel)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName,
                ValueData = valueData,
                ValueKind = RegistryValueKind.String
            };

            return location == RegistryValueLocation.CurrentUser
                ? ExecuteLocal(request, ElevatedOperations.RegistryWrite)
                    .WithPrivilegeDetails(PrivilegeLevel.Standard, CurrentOnlyLevels)
                : ExecutePrivileged(request, ElevatedOperations.RegistryWrite, privilegeLevel)
                    .WithPrivilegeDetails(
                        GetEffectiveExplicitPrivilegeLevel(privilegeLevel),
                        new[] { privilegeLevel });
        }

        public PrivilegeExecutionResult DeleteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName)
        {
            return DeleteString(
                location,
                subKeyPath,
                valueName,
                RegistryPrivilegePolicy.Auto);
        }

        public PrivilegeExecutionResult DeleteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            RegistryPrivilegePolicy privilegePolicy)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName
            };

            return ExecuteWithPolicy(request, ElevatedOperations.RegistryDelete, privilegePolicy);
        }

        public PrivilegeExecutionResult DeleteString(
            RegistryValueLocation location,
            string subKeyPath,
            string valueName,
            PrivilegeLevel privilegeLevel)
        {
            var request = new RegistryValueWriteRequest
            {
                Location = location,
                SubKeyPath = subKeyPath,
                ValueName = valueName
            };

            return location == RegistryValueLocation.CurrentUser
                ? ExecuteLocal(request, ElevatedOperations.RegistryDelete)
                    .WithPrivilegeDetails(PrivilegeLevel.Standard, CurrentOnlyLevels)
                : ExecutePrivileged(request, ElevatedOperations.RegistryDelete, privilegeLevel)
                    .WithPrivilegeDetails(
                        GetEffectiveExplicitPrivilegeLevel(privilegeLevel),
                        new[] { privilegeLevel });
        }

        internal static PrivilegeLevel[] GetAttemptLevels(
            RegistryValueLocation location,
            RegistryPrivilegePolicy privilegePolicy)
        {
            if (location == RegistryValueLocation.CurrentUser
                || privilegePolicy == RegistryPrivilegePolicy.CurrentUserOnly)
            {
                return CurrentOnlyLevels;
            }

            return privilegePolicy == RegistryPrivilegePolicy.AutoWithoutTI
                ? AutoWithoutTrustedInstallerLevels
                : AutoLevels;
        }

        internal static bool ShouldTryNextPrivilege(PrivilegeExecutionResult result)
        {
            return result != null
                && result.Status == PrivilegeExecutionStatus.Failed
                && string.Equals(result.ErrorCode, PrivilegeErrorCodes.RegistryAccessDenied, StringComparison.Ordinal);
        }

        private PrivilegeExecutionResult ExecuteWithPolicy(
            RegistryValueWriteRequest request,
            string operationName,
            RegistryPrivilegePolicy privilegePolicy)
        {
            var levels = GetAttemptLevels(request.Location, privilegePolicy);
            var attemptedLevels = new List<PrivilegeLevel>(levels.Length);
            PrivilegeExecutionResult lastResult = null;

            foreach (var privilegeLevel in levels)
            {
                attemptedLevels.Add(privilegeLevel);
                lastResult = _operationAttempt(request, operationName, privilegeLevel);
                if (lastResult == null)
                {
                    lastResult = PrivilegeExecutionResult.Failure(
                        PrivilegeErrorCodes.EmptyAgentResponse,
                        "The registry operation returned no response.");
                }

                if (lastResult.Succeeded)
                    return lastResult.WithPrivilegeDetails(privilegeLevel, attemptedLevels.ToArray());

                if (!ShouldTryNextPrivilege(lastResult))
                    return lastResult.WithPrivilegeDetails(null, attemptedLevels.ToArray());
            }

            return lastResult?.WithPrivilegeDetails(null, attemptedLevels.ToArray())
                ?? PrivilegeExecutionResult.Failure(
                    PrivilegeErrorCodes.InvalidRequest,
                    "The registry operation did not produce a result.")
                    .WithPrivilegeDetails(null, attemptedLevels.ToArray());
        }

        private PrivilegeExecutionResult ExecuteAttempt(
            RegistryValueWriteRequest request,
            string operationName,
            PrivilegeLevel privilegeLevel)
        {
            return privilegeLevel == PrivilegeLevel.Standard
                ? ExecuteLocal(request, operationName)
                : ExecutePrivileged(request, operationName, privilegeLevel);
        }

        private static PrivilegeExecutionResult ExecuteLocal(
            RegistryValueWriteRequest request,
            string operationName)
        {
            try
            {
                ExecuteLocalRegistryOperation(request, operationName);
                return PrivilegeExecutionResult.Success();
            }
            catch (Exception exception) when (ElevatedOperationExecutor.IsPermissionFailure(exception))
            {
                return PrivilegeExecutionResult.Failure(PrivilegeErrorCodes.RegistryAccessDenied, exception.Message);
            }
            catch (Exception exception)
            {
                return PrivilegeExecutionResult.Failure(GetRegistryErrorCode(operationName), exception.Message);
            }
        }

        private PrivilegeExecutionResult ExecutePrivileged(
            RegistryValueWriteRequest request,
            string operationName,
            PrivilegeLevel privilegeLevel)
        {
            if (privilegeLevel == PrivilegeLevel.Standard)
            {
                return PrivilegeExecutionResult.Failure(
                    PrivilegeErrorCodes.PrivilegeLevelRequired,
                    "A machine-level registry write must declare an elevated privilege level.");
            }

            if (_privilegeBroker == null)
            {
                return PrivilegeExecutionResult.Unavailable(
                    PrivilegeErrorCodes.ElevatedAgentUnavailable,
                    "The elevated agent is not configured.");
            }

            var payload = DataContractPayloadSerializer.Serialize(request);
            var command = new ElevatedCommandRequest
            {
                OperationName = operationName,
                Payload = payload,
                PrivilegeLevel = privilegeLevel,
                RequestId = Guid.NewGuid().ToString("N")
            };

            return _privilegeBroker.Execute(command);
        }

        private static void ExecuteLocalRegistryOperation(
            RegistryValueWriteRequest request,
            string operationName)
        {
            if (string.Equals(operationName, ElevatedOperations.RegistryWrite, StringComparison.OrdinalIgnoreCase))
            {
                WindowsRegistryWriter.WriteValue(request);
                return;
            }

            if (string.Equals(operationName, ElevatedOperations.RegistryDelete, StringComparison.OrdinalIgnoreCase))
            {
                WindowsRegistryWriter.DeleteValue(request);
                return;
            }

            throw new InvalidOperationException("The registry operation is not supported.");
        }

        private static string GetRegistryErrorCode(string operationName)
        {
            return string.Equals(operationName, ElevatedOperations.RegistryDelete, StringComparison.OrdinalIgnoreCase)
                ? PrivilegeErrorCodes.RegistryDeleteFailed
                : PrivilegeErrorCodes.RegistryWriteFailed;
        }

        private static PrivilegeLevel? GetEffectiveExplicitPrivilegeLevel(PrivilegeLevel privilegeLevel)
        {
            return privilegeLevel == PrivilegeLevel.Standard
                ? null
                : privilegeLevel;
        }
    }
}
