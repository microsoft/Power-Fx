// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Types;

namespace Microsoft.PowerFx
{
    /// <summary>
    /// Definition for User object. 
    /// - Names of C# members match the names we get in Power Fx's User object. 
    /// - This has a curated set of user identifiers. This list can grow (hence this is a class, not an interface). 
    /// - Host can specify which symbols are available, and leave these blank if not supported on this host. 
    /// - Some information may require a network call to fetch, so we use task and virtual. Different properties may require different fetches, so let host override each property (rather than a single virtual).
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Name of the User object as it shows up in a Power Fx expression.
        /// </summary>
        public const string ObjectName = "User";

        // Host should have specified the symbol wasn't available, so formulas shouldn't hit this at runtime. 
        private static NotSupportedException Ex([CallerMemberName] string caller = null)
        {
            return new NotSupportedException($"User.{caller} is not supported.");
        }

        public virtual async Task<string> FullName(CancellationToken cancel = default) => throw Ex();

        public virtual async Task<string> Email(CancellationToken cancel = default) => throw Ex();

        /// <summary>
        /// Dataverse User Table Id, coming from IExecutionContext.UserId. 
        /// This is a key into 'systemuser' table.
        /// </summary>
        public virtual async Task<Guid> DataverseUserTableId(CancellationToken cancel = default) => throw Ex();

        /// <summary>
        /// TBD - For Cards / PVA. 
        /// </summary>
        public virtual async Task<Guid> BotMemberId(CancellationToken cancel = default) => throw Ex();

        /// <summary>
        /// Get the Power Fx type for a User object that has the given fields exposed. 
        /// </summary>
        /// <param name="fields">Valid subset of fields on user object.</param>
        /// <returns></returns>
        public static FormulaType GetUserType(params string[] fields)
        {
            // User is currently a record, but other host objects (like 'Host') are actually
            // non-record types, and we may move User in that direction. So return type is 
            // FormulaType, not RecordType. 
            return GetUserTypeWorker(fields);
        }

        internal static RecordType GetUserTypeWorker(params string[] fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            if (fields.Length == 0)
            {
                throw new InvalidOperationException($"Must have at least 1 field for User object.");
            }

            var type = RecordType.Empty();
            HashSet<string> added = new HashSet<string>();

            foreach (var field in fields)
            {
                if (!added.Add(field))
                {
                    // Dtype.Add will assert on duplicate add. Need to explicitly catch. 
                    throw new InvalidOperationException($"Field '{0}' is already added.");
                }

                if (field == nameof(FullName) ||
                    field == nameof(Email))
                {
                    type = type.Add(new NamedFormulaType(field, FormulaType.String));
                } 
                else if (
                    field == nameof(DataverseUserTableId) ||
                    field == nameof(BotMemberId))
                {
                    type = type.Add(new NamedFormulaType(field, FormulaType.Guid));
                }
                else
                {
                    throw new InvalidOperationException($"Field '{field}' is not supported on User object.");
                }
            }

            return type;
        }
    }

    /// <summary>
    /// Helper for implementing <see cref="UserInfo"/> with static values. 
    /// </summary>
    public class BasicUserInfo
    {
        // Same field names as UserInfo. But mutable and sync. 
        public string Email { get; set; }
        
        public string FullName { get; set; }

        public Guid DataverseUserTableId { get; set; }

        public Guid BotMemberId { get; set; }

        public UserInfo UserInfo => new Adapter(this);
        
        // for convenience, we want synchronous properties for the User fields. 
        // But UserInfo class already has async get methods for the fields. 
        // C# won't allow a property and method with same name, so use an Adapter class. 
        private class Adapter : UserInfo
        {
            private readonly BasicUserInfo _parent;

            public Adapter(BasicUserInfo parent)
            {
                _parent = parent;
            }

            public override async Task<string> FullName(CancellationToken cancel) => _parent.FullName;

            public override async Task<string> Email(CancellationToken cancel) => _parent.Email;

            public override async Task<Guid> DataverseUserTableId(CancellationToken cancel) => _parent.DataverseUserTableId;

            public override async Task<Guid> BotMemberId(CancellationToken cancel) => _parent.BotMemberId;
        }
    }
}
