namespace Xrm.Utils.Core.Common.Interfaces
{
    using System;
    using Xrm.Utils.Core.Common.Misc;
    using Microsoft.Xrm.Sdk;

    /// <summary>
    /// Plugin specific extensions to IContainable
    /// </summary>
    public interface IPluginExecutionContainer : IExecutionContainer
    {
        #region Public Properties

        /// <summary>
        /// Gets instance of the <see cref="IPluginExecutionContext" /> assosiated with current container
        /// </summary>
        IPluginExecutionContext Context
        {
            get;
        }

        EntitySet Entities
        {
            get;
        }

        /// <summary>
        /// Gets instance of initial <see cref="IServiceProvider"/> assosiated with current plugin instance.
        /// </summary>
        IServiceProvider Provider
        {
            get;
        }

        #endregion Public Properties
    }
}