﻿namespace Xrm.Utils.Core.Common.Fluent.Access
{
    using Xrm.Utils.Core.Common.Extensions;
    using Xrm.Utils.Core.Common.Interfaces;
    using Microsoft.Crm.Sdk.Messages;
    using Microsoft.Xrm.Sdk;

    public class OperationsSet2 : Information
    {
        #region Internal Constructors

        internal OperationsSet2(IExecutionContainer container, EntityReference principal, EntityReference target)
            : base(container, principal, target)
        {
        }

        #endregion Internal Constructors

        #region Public Methods

        /// <summary>
        /// Assigns current record to given principal
        /// </summary>
        /// <returns></returns>
        public bool Assign()
        {
            try
            {
                container.Service.Execute(new AssignRequest()
                {
                    Assignee = principal,
                    Target = target
                });
                container.Log($"Assigned {target.LogicalName} to {principal.LogicalName} {principal.Id}");

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Grants access to current record to given principal
        /// </summary>
        /// <param name="accessMask"></param>
        /// <returns></returns>
        public bool Grant(AccessRights accessMask)
        {
            try
            {
                container.Service.Execute(new GrantAccessRequest()
                {
                    PrincipalAccess = new PrincipalAccess()
                    {
                        Principal = principal,
                        AccessMask = accessMask
                    },
                    Target = target
                });

                container.Log($"{principal.LogicalName}:{principal.Id} was granted {accessMask} to {target.LogicalName}:{target.Id}");

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Revokes access from current record from given principal
        /// </summary>
        /// <returns></returns>
        public bool Revoke()
        {
            try
            {
                container.Service.Execute(new RevokeAccessRequest()
                {
                    Revokee = principal,
                    Target = target
                });

                container.Log($"{principal.LogicalName}:{principal.Id} was revoked from {target.LogicalName}:{target.Id}");

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Public Methods
    }
}