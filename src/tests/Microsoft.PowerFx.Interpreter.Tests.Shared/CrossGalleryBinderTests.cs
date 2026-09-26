// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PowerFx.Core.App.Controls;
using Microsoft.PowerFx.Core.Binding;
using Microsoft.PowerFx.Core.Errors;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Parser;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;

namespace Microsoft.PowerFx.Interpreter.Tests
{
    /// <summary>
    /// Tests for the Binder warning that fires when a control outside a gallery
    /// references a control inside it (implicit cross-gallery/form reference).
    /// </summary>
    public class CrossGalleryBinderTests : IDisposable
    {
        private readonly IExternalStringResources _previousResources;

        public CrossGalleryBinderTests()
        {
            // Save and set external string resources so the warning key resolves
            _previousResources = StringResources.ExternalStringResources;
            StringResources.ExternalStringResources = new TestStringResources();
        }

        public void Dispose()
        {
            StringResources.ExternalStringResources = _previousResources;
        }

        [Fact]
        public void CrossGalleryReference_ProducesWarning()
        {
            // Arrange: Gallery1 contains Button1 (replicable). Label1 is outside the gallery.
            var galleryTemplate = new DummyTemplate { ActiveItemPropertyName = "Selected" };
            var galleryControl = new DummyExternalControl
            {
                DisplayName = "Gallery1",
                Template = galleryTemplate
            };

            var button1 = new DummyExternalControl
            {
                DisplayName = "Button1",
                IsReplicable = true,
                ReplicatingParent = galleryControl,
                Template = new DummyTemplate()
            };

            var label1 = new DummyExternalControl
            {
                DisplayName = "Label1",
                Template = new DummyTemplate()
            };

            var symbolTable = new MockSymbolTable();
            symbolTable.MockCurrentEntity = label1;
            symbolTable.AddControl("Gallery1", galleryControl, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Selected", DType.EmptyRecord, true));
            symbolTable.AddControl("Button1", button1, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));
            symbolTable.AddControl("Label1", label1, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));

            var parseResult = TexlParser.ParseScript("Button1.Text");

            // Act
            var binding = TexlBinding.Run(
                new MockGlue(),
                parseResult.Root,
                symbolTable,
                new BindingConfig(),
                DType.EmptyRecord);

            // Assert: should produce exactly one warning about cross-gallery reference
            var warnings = binding.ErrorContainer.GetErrors()
                .Where(e => e.Severity == DocumentErrorSeverity.Warning)
                .ToList();
            Assert.Single(warnings);
            Assert.Equal(DocumentErrorSeverity.Warning, warnings[0].Severity);
        }

        [Fact]
        public void SameGalleryReference_NoWarning()
        {
            // Arrange: Both Button1 and Label1 are inside the same Gallery1.
            var galleryTemplate = new DummyTemplate { ActiveItemPropertyName = "Selected" };
            var galleryControl = new DummyExternalControl
            {
                DisplayName = "Gallery1",
                Template = galleryTemplate
            };

            var button1 = new DummyExternalControl
            {
                DisplayName = "Button1",
                IsReplicable = true,
                ReplicatingParent = galleryControl,
                Template = new DummyTemplate()
            };

            // Label1 is also inside Gallery1 — IsDescendentOf(gallery) returns true
            var label1 = new GalleryChildControl
            {
                DisplayName = "Label1",
                IsReplicable = true,
                ReplicatingParent = galleryControl,
                ParentGallery = galleryControl,
                Template = new DummyTemplate()
            };

            var symbolTable = new MockSymbolTable();
            symbolTable.MockCurrentEntity = label1;
            symbolTable.AddControl("Gallery1", galleryControl, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Selected", DType.EmptyRecord, true));
            symbolTable.AddControl("Button1", button1, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));
            symbolTable.AddControl("Label1", label1, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));

            var parseResult = TexlParser.ParseScript("Button1.Text");

            // Act
            var binding = TexlBinding.Run(
                new MockGlue(),
                parseResult.Root,
                symbolTable,
                new BindingConfig(),
                DType.EmptyRecord);

            // Assert: no warning because both controls are in the same gallery
            var warnings = binding.ErrorContainer.GetErrors()
                .Where(e => e.Severity == DocumentErrorSeverity.Warning)
                .ToList();
            Assert.Empty(warnings);
        }

        [Fact]
        public void CrossGalleryReference_NoActiveItemProperty_NoWarning()
        {
            // Arrange: Gallery has no ActiveItemPropertyName (null).
            var galleryTemplate = new DummyTemplate { ActiveItemPropertyName = null };
            var galleryControl = new DummyExternalControl
            {
                DisplayName = "Gallery1",
                Template = galleryTemplate
            };

            var button1 = new DummyExternalControl
            {
                DisplayName = "Button1",
                IsReplicable = true,
                ReplicatingParent = galleryControl,
                Template = new DummyTemplate()
            };

            var label1 = new DummyExternalControl
            {
                DisplayName = "Label1",
                Template = new DummyTemplate()
            };

            var symbolTable = new MockSymbolTable();
            symbolTable.MockCurrentEntity = label1;
            symbolTable.AddControl("Gallery1", galleryControl);
            symbolTable.AddControl("Button1", button1, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));
            symbolTable.AddControl("Label1", label1, TypeTree.Create(new List<KeyValuePair<string, DType>>()).SetItem("Text", DType.String, true));

            var parseResult = TexlParser.ParseScript("Button1.Text");

            // Act
            var binding = TexlBinding.Run(
                new MockGlue(),
                parseResult.Root,
                symbolTable,
                new BindingConfig(),
                DType.EmptyRecord);

            // Assert: no warning because gallery has no ActiveItemPropertyName
            var warnings = binding.ErrorContainer.GetErrors()
                .Where(e => e.Severity == DocumentErrorSeverity.Warning)
                .ToList();
            Assert.Empty(warnings);
        }

        private class GalleryChildControl : DummyExternalControl
        {
            public IExternalControl ParentGallery { get; set; }

            public override bool IsDescendentOf(IExternalControl controlInfo)
            {
                return controlInfo == ParentGallery;
            }
        }

        /// <summary>
        /// Provides the WarnImplicitGallerySelectedReference error resource for tests.
        /// In production, this string comes from DocServer's Resources.pares.
        /// </summary>
        private class TestStringResources : IExternalStringResources
        {
            private const string WarningMessage = "'{0}' is inside '{1}'. Use '{1}.{2}.{0}' for an explicit reference.";

            public bool TryGet(string resourceKey, out string resourceValue, string locale = null)
            {
                if (resourceKey == "WarnImplicitGallerySelectedReference")
                {
                    resourceValue = WarningMessage;
                    return true;
                }

                resourceValue = null;
                return false;
            }

            public bool TryGetErrorResource(ErrorResourceKey resourceKey, out ErrorResource resourceValue, string locale = null)
            {
                if (resourceKey.Key == "WarnImplicitGallerySelectedReference")
                {
                    var members = new Dictionary<string, Dictionary<int, string>>
                    {
                        { ErrorResource.ShortMessageTag, new Dictionary<int, string> { { 1, WarningMessage } } }
                    };
                    resourceValue = ErrorResource.Reassemble(members);
                    return true;
                }

                resourceValue = null;
                return false;
            }
        }
    }
}
