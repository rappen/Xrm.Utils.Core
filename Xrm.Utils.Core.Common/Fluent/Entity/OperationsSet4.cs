﻿namespace Xrm.Utils.Core.Common.Fluent.Entity
{
    using System.Collections.Generic;
    using System.Linq;
    using Xrm.Utils.Core.Common.Extensions;
    using Xrm.Utils.Core.Common.Interfaces;
    using Xrm.Utils.Core.Common.Misc;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;

    public class OperationsSet4 : OperationsSet3
    {
        #region Private Fields

        private readonly string name;
        private FilterExpression condition;
        private bool flagNoLock;
        private bool flagOnlyActive;
        private IEnumerable<OrderExpression> order;

        #endregion Private Fields

        #region Public Constructors

        public OperationsSet4(IExecutionContainer container, string logicalName, EntityReference target, string name)
            : base(container, logicalName, target)
        {
            this.name = name;

            flagNoLock = false;
            flagOnlyActive = false;
        }

        #endregion Public Constructors

        #region Public Properties

        public OperationsSet4 NoLock
        {
            get
            {
                flagNoLock = true;

                return this;
            }
        }

        public OperationsSet4 OnlyActive
        {
            get
            {
                flagOnlyActive = true;

                return this;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Add locking flag explicitly
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public OperationsSet4 Active(bool value)
        {
            flagOnlyActive = value;

            return this;
        }

        /// <summary>
        /// Retrieves records (children) relating to current record (parent)
        /// </summary>
        /// <param name="columns"><see cref="ColumnSet" /> to expand</param>
        /// <returns></returns>
        public EntityCollection Expand(ColumnSet columns)
        {
            var metadata = (EntityMetadata)container.Execute(
                new RetrieveEntityRequest()
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Attributes,
                    RetrieveAsIfPublished = true
                });

            // Condition checks if given `name` is attribute on entity `logicalName`,
            // If not, `name` would be treated as an N:N relation to this entity
            return (metadata.Attributes.Any(x => x.LogicalName == name))
                ? ExpandEntity(columns)
                : ExpandRelation(columns);
        }

        /// <summary>
        /// Will add given <paramref name="columns" /> to the operation
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public EntityCollection Expand(params string[] columns) =>
            Expand(new ColumnSet(columns));

        /// <summary>
        /// Add fintering condition to composed query
        /// </summary>
        /// <param name="condition"><see cref="FilterExpression" /> to apply</param>
        /// <returns></returns>
        public OperationsSet4 FilteredBy(FilterExpression condition)
        {
            this.condition = condition;

            return this;
        }

        /// <summary>
        /// Add locking flag explicitly
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public OperationsSet4 Lock(bool value)
        {
            flagNoLock = value;

            return this;
        }

        /// <summary>
        /// Add sorting order to composed query
        /// </summary>
        /// <param name="order"><see cref="OrderExpression" /> to apply</param>
        /// <returns></returns>
        public OperationsSet4 SortedBy(OrderExpression order)
        {
            if (order != null)
            {
                this.order = new OrderExpression[] { order };
            }

            return this;
        }

        /// <summary>
        /// Add sorting order to composed query
        /// </summary>
        /// <param name="attributeName"></param>
        /// <param name="orderType"></param>
        /// <returns></returns>
        public OperationsSet4 SortedBy(string attributeName, OrderType orderType) =>
            SortedBy(new OrderExpression(attributeName, orderType));

        /// <summary>
        /// Add sorting order to composed query
        /// </summary>
        /// <param name="order">Collection of <see cref="OrderExpression" /> to apply</param>
        /// <returns></returns>
        public OperationsSet4 SortedBy(IEnumerable<OrderExpression> order)
        {
            this.order = order;

            return this;
        }

        #endregion Public Methods

        #region Private Methods

        private EntityCollection ExpandEntity(ColumnSet columns)
        {
            container.StartSection($"ExpandEntity");

            container.Log($"Searching for records in '{logicalName}' where '{name}' attribute is '{target.Id}' and active={flagOnlyActive}");

            try
            {
                var query = new QueryExpression(logicalName);
                query.Criteria.AddCondition(name, ConditionOperator.Equal, target.Id);
                if (flagOnlyActive)
                {
                    container.Log($"Adding active condition");
                    Query.AppendConditionActive(query.Criteria);
                }

                query.ColumnSet = columns;
                if (condition != null)
                {
                    container.Log($"Adding filter with {condition.Conditions.Count} conditions");
                    query.Criteria.AddFilter(condition);
                }

                if (order != null && order.Count() > 0)
                {
                    container.Log($"Adding orders ({order.Count()})");
                    query.Orders.AddRange(order);
                }

                query.NoLock = flagNoLock;
                container.Log($"Setting `NoLock` to {flagNoLock}");

                var result = container.Service.RetrieveMultiple(query);

                container.Log($"Got {result.Entities.Count} records");

                return result;
            }
            finally
            {
                container.EndSection();
            }
        }

        private EntityCollection ExpandRelation(ColumnSet columns)
        {
            container.StartSection($"Slim: GetAssociated({logicalName}, {name}, {flagOnlyActive})");

            var thisIdAttribute = container.Entity(target.LogicalName).PrimaryIdAttribute;
            var otherIdAttribute = container.Entity(logicalName).PrimaryIdAttribute;

            var query = new QueryExpression(logicalName);
            if (logicalName != target.LogicalName)
            {
                // N:N mellan olika entiteter
                var leSource = Query.AppendLinkMM(query.LinkEntities, name, logicalName, otherIdAttribute, target.LogicalName, thisIdAttribute);
                Query.AppendCondition(leSource.LinkCriteria, LogicalOperator.And, thisIdAttribute, ConditionOperator.Equal, target.Id);
            }
            else
            {
                // N:N till samma enititet
                var leSource = Query.AppendLink(query.LinkEntities, logicalName, name, otherIdAttribute, thisIdAttribute + "two");
                Query.AppendCondition(leSource.LinkCriteria, LogicalOperator.And, thisIdAttribute + "one", ConditionOperator.Equal, target.Id);
            }

            if (flagOnlyActive)
            {
                Query.AppendConditionActive(query.Criteria);
            }

            query.ColumnSet = columns;
            if (condition != null)
            {
                container.Log($"Adding filter with {condition.Conditions.Count} conditions");
                query.Criteria.AddFilter(condition);
            }

            if (order != null && order.Count() > 0)
            {
                container.Log($"Adding orders ({order.Count()})");
                query.Orders.AddRange(order);
            }

            query.NoLock = flagNoLock;
            var result = container.Service.RetrieveMultiple(query);

            container.Log($"Got {result.Entities.Count} records");
            container.EndSection();

            return result;
        }

        #endregion Private Methods
    }
}