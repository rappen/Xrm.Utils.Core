﻿namespace Xrm.Utils.Core.Common.Fluent.Access
{
    using Xrm.Utils.Core.Common.Interfaces;
    using Microsoft.Xrm.Sdk;

    public class Information : InformationBase
    {
        #region Protected Fields

        protected readonly EntityReference principal;
        protected readonly EntityReference target;

        #endregion Protected Fields

        #region Internal Constructors

        internal Information(IExecutionContainer container, EntityReference principal)
            : base(container) =>
            this.principal = principal;

        internal Information(IExecutionContainer container, EntityReference principal, EntityReference target)
            : this(container, principal) =>
            this.target = target;

        #endregion Internal Constructors
    }
}