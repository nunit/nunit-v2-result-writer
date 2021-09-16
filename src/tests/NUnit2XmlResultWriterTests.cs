// ***********************************************************************
// Copyright (c) Charlie Poole and contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Engine.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Addins
{
    public class NUnit2XmlResultWriterTests
    {
        [Test]
        public void CheckExtensionAttribute()
        {
            Assert.That(typeof(NUnit2XmlResultWriter),
                Has.Attribute<ExtensionAttribute>());
        }

        [Test]
        public void CheckExtensionPropertyAttribute()
        {
            Assert.That(typeof(NUnit2XmlResultWriter),
                Has.Attribute<ExtensionPropertyAttribute>()
                    .With.Property("Name").EqualTo("Format")
                    .And.Property("Value").EqualTo("nunit2"));
        }

        [Test, Ignore("Intentionally Ignored")]
        public void IgnoredTest()
        {
        }
    }
}
