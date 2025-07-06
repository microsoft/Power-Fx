// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Syntax;

namespace Microsoft.PowerFx.Core.Types
{
    internal enum UnitFamily
    {
        Base,
        ImperialLength,
        ImperialWeight,
    }

    internal class Units
    {
        private readonly Dictionary<string, Dimension> _dimensions = new Dictionary<string, Dimension>();
        private readonly Dictionary<string, Unit> _units = new Dictionary<string, Unit>();
        public static readonly Dimension DimensionUnknown = new Dimension("unknown");

        public Unit LookUpUnit(string name)
        {
            if (_units.TryGetValue(name, out var unit))
            {
                return unit;
            }

            return null;
        }

        public Dimension AddDimension(string name)
        {
            if (_dimensions.ContainsKey(name))
            {
                throw new ArgumentException($"Dimension '{name}' already exists.");
            }

            var dimension = new Dimension(name);
            _dimensions[name] = dimension;
            return dimension;
        }

        public Unit AddUnit(string singular, string plural, string abbreviation, Dimension dimension, decimal baseMult, UnitFamily family = UnitFamily.Base, decimal familyMult = 0)
        {
            if (_units.ContainsKey(singular))
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
                _units[singular] = unit;
            }

            if (plural != string.Empty)
            {
                _units[plural] = unit;
            }

            if (abbreviation != string.Empty)
            {
                _units[abbreviation] = unit;
            }

            return unit;
        }

        internal Units()
        {
            AddDimension("currency");

            var length = AddDimension("length");
            AddUnit("meter", "meters", "m", length, 1);
            AddUnit("kilometer", "kilometers", "km", length, 1000m);
            AddUnit("centimeter", "centimeters", "cm", length, 0.01m);
            AddUnit("millimeter", "millimeters", "mm", length, 0.001m);
            AddUnit("inch", "inches", string.Empty, length, 0.025399986m, UnitFamily.ImperialLength, 1);
            AddUnit("foot", "feet", "ft", length, 0.30479999m, UnitFamily.ImperialLength, 12);
            AddUnit("yard", "yards", "yd", length, 0.91440276m, UnitFamily.ImperialLength, 12 * 3);
            AddUnit("mile", "miles", string.Empty, length, 1609.3445m, UnitFamily.ImperialLength, 5280);

            var mass = AddDimension("mass");
            AddUnit("kilogram", "kilograms", "kg", mass, 1);
            AddUnit("gram", "grams", "gm", mass, 0.001m);
            AddUnit("milligram", "milligrams", "mg", mass, 0.00001m);
            AddUnit("pound", "pounds", "lb", mass, 0.45359237m, UnitFamily.ImperialWeight, 1);
            AddUnit("ounce", "ounces", "oz", mass, 0.02834952m, UnitFamily.ImperialWeight, 16);

            var time = AddDimension("time");
            AddUnit("second", "seconds", "sec", time, 1);
            AddUnit("millisecond", "milliseconds", "ms", time, 0.001m);
            AddUnit("minute", "minutes", "min", time, 60);
            AddUnit("hour", "hours", "hr", time, 60 * 60);
            AddUnit("day", "days", string.Empty, time, 60 * 60 * 24);
            AddUnit("week", "weeks", string.Empty, time, 60 * 60 * 24 * 7);

#if false
            // Temperature units are not supported in the current version of Power Fx. The different zero points make it too complex for a simple unit conversion.
            var temperature = AddDimension("temperature");
            AddUnit("celsius", string.Empty, "°C", temperature, 1);
            AddUnit("fahrenheit", string.Empty, "°F", temperature, 0.5555555555555556m, -32);
            AddUnit("kelvin", string.Empty, "°K", temperature, 1);
#endif

            var angle = AddDimension("angle");
            AddUnit("radian", "radians", "rad", angle, 1);
            AddUnit("degree", "degrees", "°", angle, 0.01745329252m);
            AddUnit("gradian", "gradians", "grad", angle, 0.01570796m);

            var quantity = AddDimension("quantity");
            AddUnit("unit", "units", string.Empty, quantity, 1);
        }
    }

    internal class Dimension
    {
        public string Name;
        public int Number;
        private static int _nextNumber = 0;

        public Dimension(string name)
        {
            Name = name;
            Number = _nextNumber++;
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

        public UnitInfo(Unit unit, int power)
        {
            _units = new List<(Unit, int)> { (unit, power) };
        }

        public UnitInfo(List<(Unit, int)> units)
        {
            _units = units;

            _units.Sort((a, b) => a.unit._dimension.Number - b.unit._dimension.Number);
        }

        public static UnitInfo AddUnit(UnitInfo unitInfo, Unit unit, int power = 1)
        {
            if (unitInfo == null)
            {
                return new UnitInfo(unit, power);
            }
            else
            {
                return new UnitInfo(new List<(Unit, int)>(unitInfo._units) { (unit, power) });
            }
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

            foreach (var bPair in b._units)
            {
                var (bUnit, bPower) = bPair;

                if (reciprocal)
                {
                    bPower = -bPower;
                }

                var (aUnit, aPower) = a._units.FirstOrDefault(u => u.unit._dimension == bUnit._dimension);

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

                    if (aPower != -bPower)
                    {
                        newUnits.Add((aUnit, aPower + bPower));
                    }
                }
                else
                {
                    newUnits.Add((bUnit, bPower));
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

            return (factor, newUnits.Count == 0 ? null : new UnitInfo(newUnits));
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

        public UnitInfo Power(int power)
        {
            List<(Unit unit, int power)> newUnits = new List<(Unit, int)>(_units);

            for (int i = 0; i < newUnits.Count; i++)
            {
                newUnits[i] = (newUnits[i].unit, newUnits[i].power * power);
            }

            return new UnitInfo(newUnits);
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
                dens = " / " + dens;
            }

            return $"{nums}{dens}";
        }

        public string ToUnitsString(bool plural)
        {
            var nums = string.Join(" * ", _units.Where(unit => unit.power > 0)
                                                .OrderBy(unit => unit.power)
                                                .Select(unit => $"{(plural ? unit.unit.Plural : unit.unit.Singular)}{(unit.power > 1 ? $"^{unit.power}" : string.Empty)}"));
            var dens = string.Join(" * ", _units.Where(unit => unit.power < 0)
                                                .OrderBy(unit => -unit.power)
                                                .Select(unit => $"{unit.unit.Singular}{(unit.power < -1 ? $"^{-unit.power}" : string.Empty)}"));

            if (nums == string.Empty)
            {
                nums = "1";
            }

            if (dens != string.Empty)
            {
                dens = "/" + dens;
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
                dens = "/" + dens;
            }

            return $"{nums}{dens}";
        }
    }
}
