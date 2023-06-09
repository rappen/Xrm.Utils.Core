﻿namespace Xrm.Utils.Core.Common.Misc
{
    using System;
    using System.Collections.Generic;
    using Xrm.Utils.Core.Common.Extensions;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;

    /// <summary>A class used to compare cintdynentity instances</summary>
    public sealed class EntityComparer : IComparer<Entity>
    {
        #region Public Constructors

        /// <summary>Constructor of CintDynEntity class. Takes a list of attribute names as an argument.</summary>
        /// <param name="attributes">The attributes to sort the comparison by......</param>
        public EntityComparer(params string[] attributes)
        {
            SortAttributes = new List<SortAttribute>();
            foreach (var attribute in attributes)
            {
                if (!string.IsNullOrEmpty(attribute))
                {
                    if (attribute[0] == '!')
                    {
                        SortAttributes.Add(new SortAttribute(attribute.Substring(1), OrderType.Descending));
                    }
                    else
                    {
                        SortAttributes.Add(new SortAttribute(attribute, OrderType.Ascending));
                    }
                }
            }
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>Property returning the configured sort attributes.</summary>
        /// <returns>The list of sort attributes</returns>
        public List<SortAttribute> SortAttributes
        {
            get;
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>The method that performs the actual comparison in the CintDynEntityComparer class.</summary>
        /// <param name="x">The Entity to compare.</param>
        /// <param name="y">The Entity to compare with.</param>
        /// <returns>An integer which is greater than 0 if the x parameter is greater than the y parameter, 0 if they are equal, and negative if x is less than y.</returns>
        public int Compare(Entity x, Entity y)
        {
            var result = 0;
            foreach (var sortAttribute in SortAttributes)
            {
                var xAttrValue = x.Contains(sortAttribute.Attribute) ? x.Attributes[sortAttribute.Attribute] : null;
                var yAttrValue = y.Contains(sortAttribute.Attribute) ? y.Attributes[sortAttribute.Attribute] : null;
                if (xAttrValue is EntityReference || yAttrValue is EntityReference)
                {
                    result = CompareEntityReferences(xAttrValue, yAttrValue);
                }
                else if (xAttrValue is DateTime || yAttrValue is DateTime)
                {
                    result = CompareDateTimes(xAttrValue, yAttrValue);
                }
                else if (xAttrValue is string || yAttrValue is string)
                {
                    result = string.Compare(xAttrValue as string, yAttrValue as string, StringComparison.Ordinal);
                }
                else if (xAttrValue is int || yAttrValue is int)
                {
                    result = (int)xAttrValue - (int)yAttrValue;
                }
                else
                {
                    var xstr = x.GetAttribute(sortAttribute.Attribute, string.Empty);
                    var ystr = y.GetAttribute(sortAttribute.Attribute, string.Empty);

                    result = string.Compare(xstr, ystr, StringComparison.Ordinal);
                }

                if (result != 0)
                {
                    if (sortAttribute.Type == OrderType.Descending)
                    {
                        return -result;
                    }
                    else
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        #endregion Public Methods

        #region Private Methods

        private static int CompareDateTimes(object xAttrValue, object yAttrValue)
        {
            int result;
            if (xAttrValue == null && yAttrValue == null)
            {
                return 0;
            }

            if (xAttrValue == null)
            {
                result = 1;
            }
            else if (yAttrValue == null)
            {
                result = -1;
            }
            else
            {
                var xDateTime = (DateTime)xAttrValue;
                var yDateTime = (DateTime)yAttrValue;
                result = xDateTime.CompareTo(yDateTime);
            }

            return result;
        }

        private static int CompareEntityReferences(object xAttrValue, object yAttrValue)
        {
            int result;
            var xEntRef = xAttrValue as EntityReference;
            var yEntRef = yAttrValue as EntityReference;

            if (xEntRef == null && yEntRef == null)
            {
                return 0;
            }

            if (xEntRef == null)
            {
                result = 1;
            }
            else if (yEntRef == null)
            {
                result = -1;
            }
            else
            {
                result = string.Compare(xEntRef.LogicalName, yEntRef.LogicalName, StringComparison.Ordinal);
            }

            if (result == 0)
            {
                result = xEntRef.Id.CompareTo(yEntRef.Id);
            }

            return result;
        }

        #endregion Private Methods
    }
}