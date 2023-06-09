﻿namespace Xrm.Utils.Core.Common.Interfaces
{
    using Microsoft.Xrm.Sdk;

    /// <summary>
    /// Core object that helps to keep all objects and methods needed for CRM development in
    /// package easy to access and operate
    /// </summary>
    public interface IExecutionContainer
    {
        dynamic Values
        {
            get;
        }

        /// <summary>
        /// Get instance of the <see cref="ILoggable" /> assosiated with current container
        /// </summary>
        ILoggable Logger
        {
            get;
        }

        /// <summary>
        /// Gets instance of <see cref="IOrganizationService" /> assosiated with current container
        /// </summary>
        IOrganizationService Service
        {
            get;
        }
    }
}