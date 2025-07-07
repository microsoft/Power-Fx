// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.PowerFx.Core.Texl.Builtins;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Types
{
    internal enum UnitFamily
    {
        Base,
        ImperialLength,
        ImperialWeight,
    }

    internal sealed class UnitsService
    {
        internal static IReadOnlyDictionary<string, Dimension> Dimensions { get; }

        internal static IReadOnlyDictionary<string, Unit> Units { get; }

        static UnitsService()
        {
            Dimensions = InitDimensions().ToImmutableDictionary();
            Units = InitUnits().ToImmutableDictionary();
        }

        public static Unit LookUpUnit(string name)
        {
            if (Units.TryGetValue(name, out var unit))
            {
                return unit;
            }

            return null;
        }

        private static Dictionary<string, Dimension> InitDimensions()
        {
            var dimensions = new Dictionary<string, Dimension>();

            void AddDimension(string name)
            {
                if (dimensions.ContainsKey(name))
                {
                    throw new ArgumentException($"Dimension '{name}' already exists.");
                }

                var dimension = new Dimension(name);
                dimensions[name] = dimension;
            }

            // Add predefined dimensions
            AddDimension("currency");
            AddDimension("length");
            AddDimension("mass");
            AddDimension("time");
            AddDimension("power");
            AddDimension("angle");
            AddDimension("quantity");

            return dimensions;
        }

        private static Dictionary<string, Unit> InitUnits()
        {
            var units = new Dictionary<string, Unit>();

            void AddUnit(string singular, string plural, string abbreviation, Dimension dimension, decimal baseMult, UnitFamily family = UnitFamily.Base, decimal familyMult = 0)
            {
                if (units.ContainsKey(singular))
                {
                    throw new ArgumentException($"Unit '{singular}' already exists.");
                }

                if (dimension == null)
                {
                    throw new ArgumentNullException(nameof(dimension), "Dimension cannot be null.");
                }

                var unit = new Unit(singular, plural, abbreviation, dimension, baseMult, family, familyMult);

                if (singular != string.Empty)
                {
                    units[singular] = unit;
                }

                if (plural != string.Empty)
                {
                    units[plural] = unit;
                }

                if (abbreviation != string.Empty)
                {
                    units[abbreviation] = unit;
                }
            }

            var length = Dimensions["length"];
            AddUnit("meter", "meters", "m", length, 1);
            AddUnit("kilometer", "kilometers", "km", length, 1000m);
            AddUnit("centimeter", "centimeters", "cm", length, 0.01m);
            AddUnit("millimeter", "millimeters", "mm", length, 0.001m);
            AddUnit("inch", "inches", string.Empty, length, 0.025399986m, UnitFamily.ImperialLength, 1);
            AddUnit("foot", "feet", "ft", length, 0.30479999m, UnitFamily.ImperialLength, 12);
            AddUnit("yard", "yards", "yd", length, 0.91440276m, UnitFamily.ImperialLength, 12 * 3);
            AddUnit("mile", "miles", string.Empty, length, 1609.3445m, UnitFamily.ImperialLength, 5280);

            // c in meters/second

            // force

            // torque

            // energy

            var mass = Dimensions["mass"];
            AddUnit("kilogram", "kilograms", "kg", mass, 1);
            AddUnit("gram", "grams", "gm", mass, 0.001m);
            AddUnit("milligram", "milligrams", "mg", mass, 0.00001m);
            AddUnit("pound", "pounds", "lb", mass, 0.45359237m, UnitFamily.ImperialWeight, 1);
            AddUnit("ounce", "ounces", "oz", mass, 0.02834952m, UnitFamily.ImperialWeight, 16);

            var time = Dimensions["time"];
            AddUnit("second", "seconds", "sec", time, 1);
            AddUnit("millisecond", "milliseconds", "ms", time, 0.001m);
            AddUnit("minute", "minutes", "min", time, 60);
            AddUnit("hour", "hours", "hr", time, 60 * 60);
            AddUnit("day", "days", string.Empty, time, 60 * 60 * 24);
            AddUnit("week", "weeks", string.Empty, time, 60 * 60 * 24 * 7);

            // cycles

            var power = Dimensions["power"];
            AddUnit("watt", "watts", string.Empty, power, 1);

            // horsepower, many kinds https://en.wikipedia.org/wiki/Horsepower

#if false
            // Temperature units are not supported in the current version of Power Fx. The different zero points make it too complex for a simple unit conversion.
            var temperature = _dimensions["temperature"];
            AddUnit("celsius", string.Empty, "°C", temperature, 1);
            AddUnit("fahrenheit", string.Empty, "°F", temperature, 0.5555555555555556m, -32);
            AddUnit("kelvin", string.Empty, "°K", temperature, 1);
#endif

            var angle = Dimensions["angle"];
            AddUnit("radian", "radians", "rad", angle, 1);
            AddUnit("degree", "degrees", "°", angle, 0.01745329252m);
            AddUnit("gradian", "gradians", "grad", angle, 0.01570796m);

            var quantity = Dimensions["quantity"];
            AddUnit("unit", "units", string.Empty, quantity, 1);

            return units;
        }
    }

    internal class Dimension
    {
        public string Name;

        public Dimension(string name)
        {
            Name = name;
        }
    }

    internal class Unit
    {
        public string Singular;
        public string Plural;
        public string Abbreviation;
        public Dimension _dimension;
        public decimal _baseMult;
        public UnitFamily _family;
        public decimal _familyMult;

        public Unit(string singular, string plural, string abbreviation, Dimension dimension, decimal baseMult, UnitFamily family, decimal familyMult)
        {
            Singular = singular;
            Plural = plural;
            Abbreviation = abbreviation;
            _dimension = dimension;
            _baseMult = baseMult;
            _family = family;
            _familyMult = familyMult;
        }
    }

    internal class UnitInfo
    {
        private readonly List<(Unit unit, int power)> _units;

        public bool NoUnits => _units.Count == 0;

        public UnitInfo()
        {
            _units = new List<(Unit, int)>();
        }

        public UnitInfo(Unit unit, int power)
        {
            _units = new List<(Unit, int)> { (unit, power) };
        }

        public UnitInfo(List<(Unit, int)> units)
        {
            _units = new List<(Unit, int)>();
            var dimensions = new List<Dimension>();

            // normalize units list
            foreach (var (unit, power) in units)
            {
                int index;
                if ((index = _units.FindIndex(u => u.unit == unit)) != -1)
                {
                    _units[index] = (_units[index].unit, _units[index].power + power);
                }
                else
                {
                    if (dimensions.Contains(unit._dimension))
                    {
                        throw new Exception("more than one kind of unit for dimension");
                    }
                    else
                    {
                        dimensions.Add(unit._dimension);
                    }

                    _units.Add((unit, power));
                }
            }

            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i].power == 0)
                {
                    _units.RemoveAt(i);
                }
            }

            _units.Sort((a, b) => string.Compare(a.unit._dimension.Name, b.unit._dimension.Name, StringComparison.Ordinal));
        }

        public UnitInfo AddUnit(Unit unit, int power)
        {
            List<(Unit, int)> newUnits = new List<(Unit, int)>();
            bool found = false;

            foreach (var (oldUnit, oldPower) in _units)
            {
                if (oldUnit != unit)
                {
                    newUnits.Add((oldUnit, oldPower));
                }
                else
                {
                    newUnits.Add((oldUnit, oldPower + power));
                    found = true;
                }
            }

            if (!found)
            {
                newUnits.Add((unit, power));
            }

            return new UnitInfo(newUnits);
        }

        public static (decimal, UnitInfo) Multiply(UnitInfo a, UnitInfo b, bool reciprocal)
        {
            List<(Unit unit, int power)> newUnits = new List<(Unit, int)>();

            decimal factor = 1;

            // check b first, to avoid checking for b being null in the a check
            if (b == null)
            {
                return (1, a);
            }

            if (a == null)
            {
                if (reciprocal)
                { 
                    foreach (var bPair in b._units)
                    {
                        var (bUnit, bPower) = bPair;
                        newUnits.Add((bUnit, -bPower));
                    }

                    return (1, new UnitInfo(newUnits));
                }
                else
                {
                    return (1, b);
                }       
            }

            foreach (var (bUnit, bPower) in b._units)
            {
                var (aUnit, aPower) = a._units.FirstOrDefault(u => u.unit._dimension == bUnit._dimension);
                var bPowerRecip = reciprocal ? -bPower : bPower;

                if (aUnit != null)
                {
                    if (aUnit != bUnit)
                    {
                        decimal aMult;
                        decimal bMult;

                        if (bUnit._family == aUnit._family && bUnit._family != UnitFamily.Base)
                        {
                            aMult = aUnit._familyMult;
                            bMult = bUnit._familyMult;
                        }
                        else
                        {
                            aMult = aUnit._baseMult;
                            bMult = bUnit._baseMult;
                        }

                        for (int i = 0; i < bPower; i++)
                        {
                            factor *= bMult;
                            factor /= aMult;
                        }
                    }

                    if (aPower != -bPowerRecip)
                    {
                        newUnits.Add((aUnit, aPower + bPowerRecip));
                    }
                }
                else
                {
                    newUnits.Add((bUnit, bPowerRecip));
                }
            }

            foreach (var (aUnit, aPower) in a._units)
            {
                var (bUnit, bPower) = b._units.FirstOrDefault(u => u.unit._dimension == aUnit._dimension);

                if (bUnit == null)
                {
                    newUnits.Add((aUnit, aPower));
                }
            }

            var newUnitInfo = new UnitInfo(newUnits);

            return (factor, newUnits.Count == 0 || newUnitInfo.NoUnits ? null : newUnitInfo);
        }

        public static UnitInfo BinaryOpUnits(BinaryOpNode node, UnitInfo leftType, UnitInfo rightType)
        {
            if (node.Op == BinaryOp.Add)
            {
                return leftType;
            }
            else if (node.Op == BinaryOp.Mul || node.Op == BinaryOp.Div)
            {
                var (factor, units) = Multiply(leftType, rightType, node.Op == BinaryOp.Div);
                return units;
            }

            throw new NotImplementedException();
        }

        public static UnitInfo Sqrt(UnitInfo a)
        {
            if (a == null)
            {
                return null;
            }

            List<(Unit unit, int power)> newUnits = new List<(Unit, int)>();

            foreach (var (aUnit, aPower) in a._units)
            {
                if (aPower % 2 == 0)
                {
                    newUnits.Add((aUnit, aPower / 2));
                }
                else
                {
                    throw new Exception("square root of odd power units");
                }
            }

            return new UnitInfo(newUnits);
        }

        public static decimal Addition(UnitInfo a, UnitInfo b)
        {
            if (a == null && b == null)
            {
                return 1;
            }
            else if (a == null || b == null)
            {
                throw new Exception("unit mismatch for addition");
            }

            decimal factor = 1;

            List<(Unit unit, int power)> newUnits = new List<(Unit, int)>(a._units);

            foreach (var (bUnit, bPower) in b._units)
            {
                var (aUnit, aPower) = newUnits.FirstOrDefault(u => u.unit._dimension == bUnit._dimension);
                if (aUnit == null)
                {
                    throw new Exception("dimension not found");
                }
                else
                {
                    decimal aMult;
                    decimal bMult;

                    if (bUnit._family == aUnit._family && bUnit._family != UnitFamily.Base)
                    {
                        aMult = aUnit._familyMult;
                        bMult = bUnit._familyMult;
                    }
                    else
                    {
                        aMult = aUnit._baseMult;
                        bMult = bUnit._baseMult;
                    }

                    if (bPower < 0)
                    {
                        for (int i = 0; i < -bPower; i++)
                        {
                            factor /= bMult;
                            factor *= aMult;
                        }
                    }
                    else if (bPower > 0)
                    {
                        for (int i = 0; i < bPower; i++)
                        {
                            factor *= bMult;
                            factor /= aMult;
                        }
                    }
                }
            }

            return factor;
        }

        public static bool SameDimensions(UnitInfo a, UnitInfo b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            else if (a == null || b == null || a._units.Count != b._units.Count)
            {
                return false;
            }

            for (int t = 0; t < a._units.Count; t++)
            {
                if (a._units[t].unit._dimension != b._units[t].unit._dimension || a._units[t].power != b._units[t].power)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool SameUnits(UnitInfo a, UnitInfo b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            else if (a == null || b == null || a._units.Count != b._units.Count)
            {
                return false;
            }

            for (int t = 0; t < a._units.Count; t++)
            {
                if (a._units[t].unit != b._units[t].unit || a._units[t].power != b._units[t].power)
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            var nums = string.Join(" * ", _units.Where(unit => unit.power > 0)
                                                .OrderBy(unit => unit.power)
                                                .Select(unit => $"{unit.unit.Plural}({unit.unit._dimension.Name}){(unit.power > 1 ? $"^{unit.power}" : string.Empty)}"));
            var dens = string.Join(" * ", _units.Where(unit => unit.power < 0)
                                                .OrderBy(unit => -unit.power)
                                                .Select(unit => $"{unit.unit.Plural}({unit.unit._dimension.Name}){(unit.power < -1 ? $"^{-unit.power}" : string.Empty)}"));

            if (nums == string.Empty)
            {
                nums = "1";                 
            }

            if (dens != string.Empty)
            {
                dens = " / " + (dens.Contains("*") ? $"({dens})" : dens);
            }

            return $"{nums}{dens}";
        }

        public string ToUnitsString(bool plural)
        {
            var nums = string.Join("*", _units.Where(unit => unit.power > 0)
                                                .OrderBy(unit => unit.power)
                                                .Select(unit => $"{(plural ? unit.unit.Plural : unit.unit.Singular)}{(unit.power > 1 ? $"^{unit.power}" : string.Empty)}"));
            var dens = string.Join("*", _units.Where(unit => unit.power < 0)
                                                .OrderBy(unit => -unit.power)
                                                .Select(unit => $"{unit.unit.Singular}{(unit.power < -1 ? $"^{-unit.power}" : string.Empty)}"));

            if (nums == string.Empty)
            {
                nums = "1";
            }

            if (dens != string.Empty)
            {
                dens = "/" + (dens.Contains("*") ? $"({dens})" : dens);
            }

            return $"{nums}{dens}";
        }

        public string ToDimensionsString(bool plural)
        {
            var nums = string.Join(" * ", _units.Where(unit => unit.power > 0)
                                                .OrderBy(unit => unit.power)
                                                .Select(unit => $"{unit.unit._dimension.Name}{(unit.power > 1 ? $"^{unit.power}" : string.Empty)}"));
            var dens = string.Join(" * ", _units.Where(unit => unit.power < 0)
                                                .OrderBy(unit => -unit.power)
                                                .Select(unit => $"{unit.unit._dimension.Name}{(unit.power < -1 ? $"^{-unit.power}" : string.Empty)}"));

            if (nums == string.Empty)
            {
                nums = "1";
            }

            if (dens != string.Empty)
            {
                dens = "/" + (dens.Contains("*") ? $"({dens})" : dens);
            }

            return $"{nums}{dens}";
        }
    }
}
