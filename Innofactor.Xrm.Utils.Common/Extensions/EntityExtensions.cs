﻿namespace Innofactor.Xrm.Utils.Common.Extensions
{
    using System;
    using System.Linq;
    using Microsoft.Xrm.Sdk;

    /// <summary>
    /// Light-weight features inspired by CintDynEntity
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Clones entity instance to a new C# instance
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Cloned entity</returns>
        /// <remarks>Does NOT create a copy in database, just a new copy to work with in code.</remarks>
        public static Entity CloneAttributes(this Entity entity)
        {
            var clone = CloneId(entity);

            // Preparing all attributes except the one in which entity id is stored
            var attributes = entity.Attributes.Where(x => x.Key.ToLowerInvariant() != $"{clone.LogicalName}id".ToLowerInvariant() || (Guid)x.Value != clone.Id);

            foreach (var attribute in attributes)
            {
                if (!clone.Attributes.Contains(attribute.Key))
                {
                    clone.Attributes.Add(attribute);
                }
            }

            return clone;
        }

        /// <summary>
        /// Clones entity instance to a new C# instance
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>Cloned entity</returns>
        /// <remarks>Does NOT create a copy in database, just a new copy to work with in code.</remarks>
        public static Entity CloneId(this Entity entity) =>
            new Entity(entity.LogicalName, entity.Id);

        /// <summary>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="notnull"></param>
        /// <returns></returns>
        public static bool Contains(this Entity entity, string name, bool notnull) =>
            entity.Attributes.Contains(name) && (!notnull || entity.Attributes[name] != null);

        /// <summary>
        /// Generic method to retrieve property with name "name" of type "T"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="attribute"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public static T GetAttribute<T>(this Entity entity, string attribute, T @default) =>
            (T)(object)(entity.Contains(attribute) && entity[attribute] is T ? (T)entity[attribute] : @default);

        /// <summary>Gets bool indicating if record is active (writable) or inactive.</summary>
        /// <param name="entity"></param>
        /// <param name="default">Default value if statecode is missing in entity.</param>
        /// <returns></returns>
        public static bool IsActive(this Entity entity, bool @default)
        {
            try
            {
                return IsActive(entity);
            }
            catch (InvalidPluginExecutionException)
            {
                return @default;
            }
        }

        /// <summary>Returns true if entity has an active state. If statecode is not available in the attribute collection an exception is thrown.</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsActive(this Entity entity)
        {
            var result = false;

            if (!entity.Attributes.Contains("statecode"))
            {
                throw new InvalidPluginExecutionException($"Querying statecode which is not currently available for {entity.LogicalName}");
            }
            else
            {
                if (((OptionSetValue)entity.Attributes["statecode"]).Value == 0)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="container"></param>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <returns></returns>
        public static Entity Merge(this Entity entity1, Entity entity2)
        {
            var merge = entity1.CloneAttributes();
            foreach (var prop in entity2.Attributes)
            {
                if (!merge.Attributes.Contains(prop.Key))
                {
                    merge.Attributes.Add(prop);
                }
            }

            return merge;
        }

        /// <summary>
        /// Om entiteten innehåller attribut från länkade entiteter efter läsning så måste aliaset för den länkade entiteten anges.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="linkedEntityAlias"></param>
        /// <param name="name"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        public static T PropertyLinkedEntity<T>(this Entity entity, string linkedEntityAlias, string name, T @default)
        {
            var defaultValue = new AliasedValue(linkedEntityAlias, name, @default);
            var value = (T)entity.GetAttribute(linkedEntityAlias + "." + name, defaultValue).Value;
            return value;
        }

        /// <summary>
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        public static void RemoveProperty(this Entity entity, string name)
        {
            if (entity.Contains(name))
            {
                entity.Attributes.Remove(name);
            }
        }

        /// <summary>
        /// Generic method to add property with "name" and set its value of type "T" to "value"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetAttribute<T>(this Entity entity, string name, T value)
        {
            if (entity.Attributes.Contains(name))
            {
                entity.Attributes[name] = value;
            }
            else
            {
                entity.Attributes.Add(name, value);
            }
        }

        /// <summary>
        /// Sätter current user (from context) som owner på entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="user"></param>
        public static void SetOwner(this Entity entity, Guid user) =>
            entity.SetAttribute("ownerid", new EntityReference("systemuser", user));
    }
}