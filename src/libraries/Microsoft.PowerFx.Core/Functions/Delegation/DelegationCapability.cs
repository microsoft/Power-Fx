// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.PowerFx.Core.Lexer;
using Microsoft.PowerFx.Core.Utils;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    // This lightweight wrapper around DelegationCababilityConstants is used to enforce valid values for capabilities.
    internal struct DelegationCapability
    {
        private BigInteger _capabilities;
        private static readonly Lazy<Dictionary<BinaryOp, DelegationCapability>> _binaryOpToDelegationCapabilityMap =
            new Lazy<Dictionary<BinaryOp, DelegationCapability>>(
                () => new Dictionary<BinaryOp, DelegationCapability>
            {
                { BinaryOp.Equal, new DelegationCapability(Equal) },
                { BinaryOp.NotEqual, new DelegationCapability(NotEqual) },
                { BinaryOp.Less, new DelegationCapability(LessThan) },
                { BinaryOp.LessEqual, new DelegationCapability(LessThanOrEqual) },
                { BinaryOp.Greater, new DelegationCapability(GreaterThan) },
                { BinaryOp.GreaterEqual, new DelegationCapability(GreaterThanOrEqual) },
                { BinaryOp.And, new DelegationCapability(And) },
                { BinaryOp.Or, new DelegationCapability(Or) },
                { BinaryOp.In, new DelegationCapability(Contains) },
                { BinaryOp.Add, new DelegationCapability(Add) },
                { BinaryOp.Mul, new DelegationCapability(Mul) },
                { BinaryOp.Div, new DelegationCapability(Div) },
            }, isThreadSafe: true);

        private static readonly Lazy<Dictionary<UnaryOp, DelegationCapability>> _unaryOpToDelegationCapabilityMap =
            new Lazy<Dictionary<UnaryOp, DelegationCapability>>(
                () => new Dictionary<UnaryOp, DelegationCapability>
            {
                { UnaryOp.Not, new DelegationCapability(Not) },
                { UnaryOp.Minus, new DelegationCapability(Sub) },
            }, isThreadSafe: true);

        private static readonly Lazy<Dictionary<string, DelegationCapability>> _operatorToDelegationCapabilityMap =
            new Lazy<Dictionary<string, DelegationCapability>>(
                () => new Dictionary<string, DelegationCapability>
            {
                { DelegationMetadataOperatorConstants.Equal, new DelegationCapability(Equal) },
                { DelegationMetadataOperatorConstants.NotEqual, new DelegationCapability(NotEqual) },
                { DelegationMetadataOperatorConstants.Less, new DelegationCapability(LessThan) },
                { DelegationMetadataOperatorConstants.LessEqual, new DelegationCapability(LessThanOrEqual) },
                { DelegationMetadataOperatorConstants.Greater, new DelegationCapability(GreaterThan) },
                { DelegationMetadataOperatorConstants.GreaterEqual, new DelegationCapability(GreaterThanOrEqual) },
                { DelegationMetadataOperatorConstants.And, new DelegationCapability(And) },
                { DelegationMetadataOperatorConstants.Or, new DelegationCapability(Or) },
                { DelegationMetadataOperatorConstants.Contains, new DelegationCapability(Contains) },
                { DelegationMetadataOperatorConstants.IndexOf, new DelegationCapability(IndexOf) },
                { DelegationMetadataOperatorConstants.SubStringOf, new DelegationCapability(SubStringOf) },
                { DelegationMetadataOperatorConstants.Not, new DelegationCapability(Not) },
                { DelegationMetadataOperatorConstants.Year, new DelegationCapability(Year) },
                { DelegationMetadataOperatorConstants.Month, new DelegationCapability(Month) },
                { DelegationMetadataOperatorConstants.Day, new DelegationCapability(Day) },
                { DelegationMetadataOperatorConstants.Hour, new DelegationCapability(Hour) },
                { DelegationMetadataOperatorConstants.Minute, new DelegationCapability(Minute) },
                { DelegationMetadataOperatorConstants.Second, new DelegationCapability(Second) },
                { DelegationMetadataOperatorConstants.Lower, new DelegationCapability(Lower) },
                { DelegationMetadataOperatorConstants.Upper, new DelegationCapability(Upper) },
                { DelegationMetadataOperatorConstants.Trim, new DelegationCapability(Trim) },
                { DelegationMetadataOperatorConstants.Null, new DelegationCapability(Null) },
                { DelegationMetadataOperatorConstants.Date, new DelegationCapability(Date) },
                { DelegationMetadataOperatorConstants.Length, new DelegationCapability(Length) },
                { DelegationMetadataOperatorConstants.Sum, new DelegationCapability(Sum) },
                { DelegationMetadataOperatorConstants.Min, new DelegationCapability(Min) },
                { DelegationMetadataOperatorConstants.Max, new DelegationCapability(Max) },
                { DelegationMetadataOperatorConstants.Average, new DelegationCapability(Average) },
                { DelegationMetadataOperatorConstants.Count, new DelegationCapability(Count) },
                { DelegationMetadataOperatorConstants.Add, new DelegationCapability(Add) },
                { DelegationMetadataOperatorConstants.Mul, new DelegationCapability(Mul) },
                { DelegationMetadataOperatorConstants.Div, new DelegationCapability(Div) },
                { DelegationMetadataOperatorConstants.Sub, new DelegationCapability(Sub) },
                { DelegationMetadataOperatorConstants.StartsWith, new DelegationCapability(StartsWith) },
                { DelegationMetadataOperatorConstants.EndsWith, new DelegationCapability(EndsWith) },
                { DelegationMetadataOperatorConstants.CountDistinct, new DelegationCapability(CountDistinct) },
                { DelegationMetadataOperatorConstants.CdsIn, new DelegationCapability(CdsIn) },
                { DelegationMetadataOperatorConstants.Top, new DelegationCapability(Top) },
                { DelegationMetadataOperatorConstants.AsType, new DelegationCapability(AsType) },
                { DelegationMetadataOperatorConstants.ArrayLookup, new DelegationCapability(ArrayLookup) }
            }, isThreadSafe: true);

        // Supported delegatable operations.
        public static readonly BigInteger None = 0x0;
        public static readonly BigInteger Sort = 0x1;
        public static readonly BigInteger Filter = 0x2;
        public static readonly BigInteger GreaterThan = 0x4;
        public static readonly BigInteger GreaterThanOrEqual = 0x8;
        public static readonly BigInteger LessThan = 0x10;
        public static readonly BigInteger LessThanOrEqual = 0x20;
        public static readonly BigInteger And = 0x40;
        public static readonly BigInteger Or = 0x80;
        public static readonly BigInteger In = 0x100;
        public static readonly BigInteger Exactin = 0x200;
        public static readonly BigInteger Not = 0x400;
        public static readonly BigInteger Equal = 0x800;
        public static readonly BigInteger NotEqual = 0x1000;
        public static readonly BigInteger SortAscendingOnly = 0x2000;
        public static readonly BigInteger Contains = 0x4000;
        public static readonly BigInteger IndexOf = 0x8000;
        public static readonly BigInteger SubStringOf = 0x10000;
        public static readonly BigInteger Year = 0x20000;
        public static readonly BigInteger Month = 0x40000;
        public static readonly BigInteger Day = 0x80000;
        public static readonly BigInteger Hour = 0x100000;
        public static readonly BigInteger Minute = 0x200000;
        public static readonly BigInteger Second = 0x400000;
        public static readonly BigInteger Lower = 0x800000;
        public static readonly BigInteger Upper = 0x1000000;
        public static readonly BigInteger Trim = 0x2000000;
        public static readonly BigInteger Null = 0x4000000;
        public static readonly BigInteger Date = 0x8000000;
        public static readonly BigInteger Length = 0x10000000;
        public static readonly BigInteger Sum = 0x20000000;
        public static readonly BigInteger Min = 0x40000000;
        public static readonly BigInteger Max = 0x80000000;
        public static readonly BigInteger Average = 0x100000000;
        public static readonly BigInteger Count = 0x200000000;
        public static readonly BigInteger Add = 0x400000000;
        public static readonly BigInteger Sub = 0x800000000;
        public static readonly BigInteger StartsWith = 0x1000000000;
        public static readonly BigInteger Mul = 0x2000000000;
        public static readonly BigInteger Div = 0x4000000000;
        public static readonly BigInteger EndsWith = 0x8000000000;
        public static readonly BigInteger CountDistinct = 0x10000000000;
        public static readonly BigInteger CdsIn = 0x20000000000;
        public static readonly BigInteger Top = 0x40000000000;
        public static readonly BigInteger Group = 0x80000000000;
        public static readonly BigInteger AsType = 0x100000000000;
        public static readonly BigInteger ArrayLookup = 0x200000000000;

        // Please update it as max value changes.
        private static BigInteger MaxSingleCapabilityValue = ArrayLookup;

        // Indicates support all functionality.
        public static BigInteger SupportsAll
        {
            get
            {
                Contracts.Assert(MaxSingleCapabilityValue.IsPowerOfTwo);

                return MaxSingleCapabilityValue | MaxSingleCapabilityValue - 1;
            }
        }

        public DelegationCapability(BigInteger delegationCapabilities)
        {
            Contracts.Assert(IsValid(delegationCapabilities));

            _capabilities = delegationCapabilities;
        }

        public static DelegationCapability operator &(DelegationCapability lhs, DelegationCapability rhs) => new DelegationCapability(lhs.Capabilities & rhs.Capabilities);

        public static DelegationCapability operator |(DelegationCapability lhs, DelegationCapability rhs) => new DelegationCapability(lhs.Capabilities | rhs.Capabilities);

        public static DelegationCapability operator ~(DelegationCapability rhs) => new DelegationCapability(~rhs.Capabilities);

        public static implicit operator DelegationCapability(BigInteger capability)
        {
            return new DelegationCapability(capability);
        }

        public bool HasLeastOneCapability(BigInteger delegationCapability)
        {
            return (_capabilities & delegationCapability) != 0;
        }

        public bool HasCapability(BigInteger delegationCapability)
        {
            if (delegationCapability == None)
            {
                return false;
            }

            return (_capabilities & delegationCapability) == delegationCapability;
        }

        public BigInteger Capabilities => _capabilities;

        public static bool IsValid(BigInteger capabilityConstant)
        {
            return (capabilityConstant == None) ||
                !(capabilityConstant & SupportsAll).IsZero;
        }

        public static Dictionary<BinaryOp, DelegationCapability> BinaryOpToDelegationCapabilityMap => _binaryOpToDelegationCapabilityMap.Value;

        public static Dictionary<UnaryOp, DelegationCapability> UnaryOpToDelegationCapabilityMap => _unaryOpToDelegationCapabilityMap.Value;

        public static Dictionary<string, DelegationCapability> OperatorToDelegationCapabilityMap => _operatorToDelegationCapabilityMap.Value;
    }
}
