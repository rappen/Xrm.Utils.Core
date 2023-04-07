namespace Xrm.Utils.Core.Common.Fluent.Entity
{
    using Xrm.Utils.Core.Common.Interfaces;
    using Microsoft.Xrm.Sdk;

    public class OperationsSet3 : InformationBase
    {
        #region Protected Fields

        protected readonly string logicalName;
        protected readonly EntityReference target;

        #endregion Protected Fields

        #region Internal Constructors

        internal OperationsSet3(IExecutionContainer container, string logicalName, EntityReference target)
            : base(container)
        {
            this.logicalName = logicalName;
            this.target = target;
        }

        #endregion Internal Constructors

        #region Public Methods

        /// <summary>
        /// Sets <paramref name="name" /> of the attribute or relation via which association was perfomed
        /// </summary>
        /// <param name="name">Name of the attribute or relation</param>
        /// <returns></returns>
        public OperationsSet4 Via(string name) =>
            new OperationsSet4(container, logicalName, target, name);

        #endregion Public Methods
    }
}