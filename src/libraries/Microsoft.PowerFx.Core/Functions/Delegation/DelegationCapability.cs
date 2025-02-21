// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Functions.Delegation
{
    // This lightweight wrapper around DelegationCababilityConstants is used to enforce valid values for capabilities.
    [DebuggerDisplay("Delegation={DebugString}")]
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
                { DelegationMetadataOperatorConstants.ArrayLookup, new DelegationCapability(ArrayLookup) },
                { DelegationMetadataOperatorConstants.Distinct, new DelegationCapability(Distinct) },
                { DelegationMetadataOperatorConstants.JoinInner, new DelegationCapability(JoinInner) },
                { DelegationMetadataOperatorConstants.JoinLeft, new DelegationCapability(JoinLeft) },
                { DelegationMetadataOperatorConstants.JoinRight, new DelegationCapability(JoinRight) },
                { DelegationMetadataOperatorConstants.JoinFull, new DelegationCapability(JoinFull) },
                { DelegationMetadataOperatorConstants.OdataExpand, new DelegationCapability(OdataExpand) }
            }, isThreadSafe: true);

        // Supported delegatable operations.
        public static readonly BigInteger None = BigInteger.Zero;
        public static readonly BigInteger Sort = BigInteger.One;                                //               0x1
        public static readonly BigInteger Filter = BigInteger.Pow(2, 1);                        //               0x2
        public static readonly BigInteger GreaterThan = BigInteger.Pow(2, 2);                   //               0x4
        public static readonly BigInteger GreaterThanOrEqual = BigInteger.Pow(2, 3);            //               0x8
        public static readonly BigInteger LessThan = BigInteger.Pow(2, 4);                      //              0x10
        public static readonly BigInteger LessThanOrEqual = BigInteger.Pow(2, 5);               //              0x20
        public static readonly BigInteger And = BigInteger.Pow(2, 6);                           //              0x40
        public static readonly BigInteger Or = BigInteger.Pow(2, 7);                            //              0x80
        public static readonly BigInteger In = BigInteger.Pow(2, 8);                            //             0x100
        public static readonly BigInteger Exactin = BigInteger.Pow(2, 9);                       //             0x200
        public static readonly BigInteger Not = BigInteger.Pow(2, 10);                          //             0x400
        public static readonly BigInteger Equal = BigInteger.Pow(2, 11);                        //             0x800
        public static readonly BigInteger NotEqual = BigInteger.Pow(2, 12);                     //            0x1000
        public static readonly BigInteger SortAscendingOnly = BigInteger.Pow(2, 13);            //            0x2000
        public static readonly BigInteger Contains = BigInteger.Pow(2, 14);                     //            0x4000
        public static readonly BigInteger IndexOf = BigInteger.Pow(2, 15);                      //            0x8000
        public static readonly BigInteger SubStringOf = BigInteger.Pow(2, 16);                  //           0x10000
        public static readonly BigInteger Year = BigInteger.Pow(2, 17);                         //           0x20000
        public static readonly BigInteger Month = BigInteger.Pow(2, 18);                        //           0x40000
        public static readonly BigInteger Day = BigInteger.Pow(2, 19);                          //           0x80000
        public static readonly BigInteger Hour = BigInteger.Pow(2, 20);                         //          0x100000
        public static readonly BigInteger Minute = BigInteger.Pow(2, 21);                       //          0x200000
        public static readonly BigInteger Second = BigInteger.Pow(2, 22);                       //          0x400000
        public static readonly BigInteger Lower = BigInteger.Pow(2, 23);                        //          0x800000
        public static readonly BigInteger Upper = BigInteger.Pow(2, 24);                        //         0x1000000
        public static readonly BigInteger Trim = BigInteger.Pow(2, 25);                         //         0x2000000
        public static readonly BigInteger Null = BigInteger.Pow(2, 26);                         //         0x4000000
        public static readonly BigInteger Date = BigInteger.Pow(2, 27);                         //         0x8000000
        public static readonly BigInteger Length = BigInteger.Pow(2, 28);                       //        0x10000000
        public static readonly BigInteger Sum = BigInteger.Pow(2, 29);                          //        0x20000000
        public static readonly BigInteger Min = BigInteger.Pow(2, 30);                          //        0x40000000
        public static readonly BigInteger Max = BigInteger.Pow(2, 31);                          //        0x80000000
        public static readonly BigInteger Average = BigInteger.Pow(2, 32);                      //       0x100000000
        public static readonly BigInteger Count = BigInteger.Pow(2, 33);                        //       0x200000000
        public static readonly BigInteger Add = BigInteger.Pow(2, 34);                          //       0x400000000
        public static readonly BigInteger Sub = BigInteger.Pow(2, 35);                          //       0x800000000
        public static readonly BigInteger StartsWith = BigInteger.Pow(2, 36);                   //      0x1000000000
        public static readonly BigInteger Mul = BigInteger.Pow(2, 37);                          //      0x2000000000
        public static readonly BigInteger Div = BigInteger.Pow(2, 38);                          //      0x4000000000
        public static readonly BigInteger EndsWith = BigInteger.Pow(2, 39);                     //      0x8000000000
        public static readonly BigInteger CountDistinct = BigInteger.Pow(2, 40);                //     0x10000000000
        public static readonly BigInteger CdsIn = BigInteger.Pow(2, 41);                        //     0x20000000000
        public static readonly BigInteger Top = BigInteger.Pow(2, 42);                          //     0x40000000000
        public static readonly BigInteger Group = BigInteger.Pow(2, 43);                        //     0x80000000000
        public static readonly BigInteger AsType = BigInteger.Pow(2, 44);                       //    0x100000000000
        public static readonly BigInteger ArrayLookup = BigInteger.Pow(2, 45);                  //    0x200000000000
        public static readonly BigInteger Distinct = BigInteger.Pow(2, 46);                     //    0x400000000000
        public static readonly BigInteger JoinInner = BigInteger.Pow(2, 47);                    //    0x800000000000
        public static readonly BigInteger JoinLeft = BigInteger.Pow(2, 48);                     //   0x1000000000000
        public static readonly BigInteger JoinRight = BigInteger.Pow(2, 49);                    //   0x2000000000000
        public static readonly BigInteger JoinFull = BigInteger.Pow(2, 50);                     //   0x4000000000000
        public static readonly BigInteger OdataExpand = BigInteger.Pow(2, 51);                  //   0x8000000000000

        // Please update it as max value changes.
        private static BigInteger maxSingleCapabilityValue = OdataExpand;

        // Indicates support all functionality.
        public static BigInteger SupportsAll
        {
            get
            {
                Contracts.Assert(maxSingleCapabilityValue.IsPowerOfTwo);

                return maxSingleCapabilityValue | maxSingleCapabilityValue - 1;
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

        public static implicit operator DelegationCapability(BigInteger capability) => new DelegationCapability(capability);

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

        internal string DebugString
        {
            get
            {
                if (_capabilities.IsZero)
                {
                    return nameof(None);
                }

                StringBuilder sb = new StringBuilder();

                if (HasCapability(Sort))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Sort));
                }

                if (HasCapability(Filter))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Filter));
                }

                if (HasCapability(GreaterThan))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(GreaterThan));
                }

                if (HasCapability(GreaterThanOrEqual))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(GreaterThanOrEqual));
                }

                if (HasCapability(LessThan))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(LessThan));
                }

                if (HasCapability(LessThanOrEqual))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(LessThanOrEqual));
                }

                if (HasCapability(And))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(And));
                }

                if (HasCapability(Or))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Or));
                }

                if (HasCapability(In))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(In));
                }

                if (HasCapability(Exactin))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Exactin));
                }

                if (HasCapability(Not))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Not));
                }

                if (HasCapability(Equal))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Equal));
                }

                if (HasCapability(NotEqual))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(NotEqual));
                }

                if (HasCapability(SortAscendingOnly))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(SortAscendingOnly));
                }

                if (HasCapability(Contains))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Contains));
                }

                if (HasCapability(IndexOf))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(IndexOf));
                }

                if (HasCapability(SubStringOf))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(SubStringOf));
                }

                if (HasCapability(Year))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Year));
                }

                if (HasCapability(Month))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Month));
                }

                if (HasCapability(Day))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Day));
                }

                if (HasCapability(Hour))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Hour));
                }

                if (HasCapability(Minute))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Minute));
                }

                if (HasCapability(Second))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Second));
                }

                if (HasCapability(Lower))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Lower));
                }

                if (HasCapability(Upper))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Upper));
                }

                if (HasCapability(Trim))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Trim));
                }

                if (HasCapability(Null))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Null));
                }

                if (HasCapability(Date))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Date));
                }

                if (HasCapability(Length))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Length));
                }

                if (HasCapability(Sum))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Sum));
                }

                if (HasCapability(Min))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Min));
                }

                if (HasCapability(Max))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Max));
                }

                if (HasCapability(Average))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Average));
                }

                if (HasCapability(Count))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Count));
                }

                if (HasCapability(Add))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Add));
                }

                if (HasCapability(Sub))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Sub));
                }

                if (HasCapability(StartsWith))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(StartsWith));
                }

                if (HasCapability(Mul))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Mul));
                }

                if (HasCapability(Div))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Div));
                }

                if (HasCapability(EndsWith))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(EndsWith));
                }

                if (HasCapability(CountDistinct))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(CountDistinct));
                }

                if (HasCapability(CdsIn))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(CdsIn));
                }

                if (HasCapability(Top))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Top));
                }

                if (HasCapability(Group))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Group));
                }

                if (HasCapability(AsType))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(AsType));
                }

                if (HasCapability(ArrayLookup))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(ArrayLookup));
                }

                if (HasCapability(Distinct))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(Distinct));
                }

                if (HasCapability(JoinInner))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(JoinInner));
                }

                if (HasCapability(JoinLeft))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(JoinLeft));
                }

                if (HasCapability(JoinRight))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(JoinRight));
                }

                if (HasCapability(JoinFull))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(JoinFull));
                }

                if (HasCapability(OdataExpand))
                {
                    AddCommaIfNeeded(sb);
                    sb.Append(nameof(OdataExpand));
                }

                return sb.ToString();
            }
        }

        private static void AddCommaIfNeeded(StringBuilder sb)
        {
            if (sb.Length != 0)
            {
                sb.Append(", ");
            }
        }

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
