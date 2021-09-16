// ***********************************************************************
// Copyright (c) Charlie Poole and contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

public abstract class Constraint<TActual>
{
    public abstract bool Matches(TActual actual);
    public string Message { get; protected set; }

    internal static string VAL<T>(T arg) => arg is string ? $"\"{arg}\"" : arg.ToString();
}

public class EqualConstraint<TActual> : Constraint<TActual>
{
    // Expected and actual types must match, possibly through conversion
    private TActual _expected;

    public EqualConstraint(TActual expected)
    {
        _expected = expected;
    }

    public override bool Matches(TActual actual)
    {
        if (_expected.Equals(actual))
            return true;

        Message = $"Expected: {VAL(_expected)} But was: {VAL(actual)}";

        return false;
    }
}

public class GreaterThanConstraint<TActual> : Constraint<TActual> where TActual : IComparable
{
    // Expected and actual types must match, possibly through conversion
    private TActual _expected;

    public GreaterThanConstraint(TActual expected)
    {
        _expected = expected;
    }

    public override bool Matches(TActual actual)
    {
        if (actual.CompareTo(_expected) > 0)
            return true;

        Message = $"Expected: value > {VAL(_expected)} But was: {VAL(actual)}";

        return false;
    }
}

public class XmlElementConstraint : Constraint<XmlNode>
{
    private string _name;
    private int _expectedCount;

    public XmlElementConstraint(string name, int expectedCount = -1)
    {
        _name = name;
        _expectedCount = expectedCount;
    }

    public override bool Matches(XmlNode actual)
    {
        XmlNodeList elements = actual.SelectNodes(_name);

        if (_expectedCount < 0) // No count specified
        {
            if (elements.Count == 0)
            {
                Message = $"Expected element <{_name}> was not found.";
                return false;
            }

            Message = $"Element <{actual.Name}> contains an element <{_name}>";
        }
        else // Count was specified
        {
            if (elements.Count != _expectedCount)
            {
                Message = $"Expected {_expectedCount} <{_name}> elements but found {elements.Count}.";
                return false;
            }

            Message = $"Element {actual.Name} has exactly {elements.Count} {_name} elements.";
        }

        return true;
    }
}

public class XmlAttributeConstraint : Constraint<XmlNode>
{
    private string _name;
    private string _value;

    public XmlAttributeConstraint(string name)
    {
        _name = name;
    }

    public override bool Matches(XmlNode actual)
    {
        var attr = actual.Attributes[_name];

        if (attr == null)
        {
            var xml = actual.OuterXml;
            int end = xml.IndexOf('>');
            if (end > 0) xml = xml.Substring(0, end + 1);
            Message = $"Expected: XmlNode with attribute {VAL(_name)}\r\nBut was: {xml}";
            return false;
        }

        if (_value != null && attr.Value != _value)
        {
            Message = $"Expected: {_name}={VAL(_value)} But was: {VAL(attr.Value)}";
            return false;
        }

        return true;
    }

    public XmlAttributeConstraint EqualTo(string value)
    {
        _value = value;
        return this;
    }
}

public static class Is
{
    public static EqualConstraint<T> EqualTo<T>(T expected) => new EqualConstraint<T>(expected);
    public static GreaterThanConstraint<T> GreaterThan<T>(T expected) where T : IComparable
    {
        return new GreaterThanConstraint<T>(expected);
    }
}

public static class Has
{
    public static XmlElementConstraint Element(string name, int expectedCount = -1) => new XmlElementConstraint(name, expectedCount);
    public static XmlAttributeConstraint Attribute(string name) => new XmlAttributeConstraint(name);
    public static HasExactly Exactly(int count) => new HasExactly(count);
    public static HasExactly One => new HasExactly(1);

    public class HasExactly
    {
        private int _count;
        public HasExactly(int count) { _count = count; }
        public XmlElementConstraint Element(string name) => new XmlElementConstraint(name, _count);
    }
}
