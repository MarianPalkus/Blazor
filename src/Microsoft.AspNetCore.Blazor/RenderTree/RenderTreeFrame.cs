﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    /// <summary>
    /// Represents an entry in a tree of user interface (UI) items.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct RenderTreeFrame
    {
        // Note that the struct layout has to be valid in both 32-bit and 64-bit runtime platforms,
        // which means that all reference-type fields need to take up 8 bytes (except for the last
        // one, which will be sized as either 4 or 8 bytes depending on the runtime platform).
        // This is not optimal for the Mono-WebAssembly case because that's always 32-bit so the
        // reference-type fields could be reduced to 4 bytes each. We could use ifdefs to have
        // different fields offsets for the 32 and 64 bit compile targets, but then we'd have the
        // complexity of needing different binaries when loaded into Mono-WASM vs desktop.
        // Eventually we might stop using this shared memory interop altogether (and would have to
        // if running as a web worker) so for now to keep things simple, treat reference types as
        // 8 bytes here.

        // --------------------------------------------------------------------------------
        // Common
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Gets the sequence number of the frame. Sequence numbers indicate the relative source
        /// positions of the instructions that inserted the frames. Sequence numbers are only
        /// comparable within the same sequence (typically, the same source method).
        /// </summary>
        [FieldOffset(0)] public readonly int Sequence;

        /// <summary>
        /// Describes the type of this frame.
        /// </summary>
        [FieldOffset(4)] public readonly RenderTreeFrameType FrameType;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Element
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>
        /// gets the number of frames in the subtree for which this frame is the root.
        /// The value is zero if the frame has not yet been closed.
        /// </summary>
        [FieldOffset(8)] public readonly int ElementSubtreeLength;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Element"/>,
        /// gets a name representing the type of the element. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        [FieldOffset(16)] public readonly string ElementName;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Text
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Text"/>,
        /// gets the content of the text frame. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        [FieldOffset(16)] public readonly string TextContent;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Attribute
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute name. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        [FieldOffset(16)] public readonly string AttributeName;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Attribute"/>,
        /// gets the attribute value. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        [FieldOffset(24)] public readonly object AttributeValue;

        // --------------------------------------------------------------------------------
        // RenderTreeFrameType.Component
        // --------------------------------------------------------------------------------

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>
        /// gets the number of frames in the subtree for which this frame is the root.
        /// The value is zero if the frame has not yet been closed.
        /// </summary>
        [FieldOffset(8)] public readonly int ComponentSubtreeLength;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance identifier.
        /// </summary>
        [FieldOffset(12)] public readonly int ComponentId;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the type of the child component.
        /// </summary>
        [FieldOffset(16)] public readonly Type ComponentType;

        /// <summary>
        /// If the <see cref="FrameType"/> property equals <see cref="RenderTreeFrameType.Component"/>,
        /// gets the child component instance. Otherwise, the value is <see langword="null"/>.
        /// </summary>
        [FieldOffset(24)] public readonly IComponent Component;

        private RenderTreeFrame(int sequence, string elementName, int elementSubtreeLength)
            : this()
        {
            FrameType = RenderTreeFrameType.Element;
            Sequence = sequence;
            ElementName = elementName;
            ElementSubtreeLength = elementSubtreeLength;
        }

        private RenderTreeFrame(int sequence, Type componentType, int componentSubtreeLength)
            : this()
        {
            FrameType = RenderTreeFrameType.Component;
            Sequence = sequence;
            ComponentType = componentType;
            ComponentSubtreeLength = componentSubtreeLength;
        }

        private RenderTreeFrame(int sequence, Type componentType, int subtreeLength, int componentId, IComponent component)
            : this(sequence, componentType, subtreeLength)
        {
            ComponentId = componentId;
            Component = component;
        }

        private RenderTreeFrame(int sequence, string textContent)
            : this()
        {
            FrameType = RenderTreeFrameType.Text;
            Sequence = sequence;
            TextContent = textContent;
        }

        private RenderTreeFrame(int sequence, string attributeName, object attributeValue)
            : this()
        {
            FrameType = RenderTreeFrameType.Attribute;
            Sequence = sequence;
            AttributeName = attributeName;
            AttributeValue = attributeValue;
        }

        internal static RenderTreeFrame Element(int sequence, string elementName)
            => new RenderTreeFrame(sequence, elementName: elementName, elementSubtreeLength: 0);

        internal static RenderTreeFrame Text(int sequence, string textContent)
            => new RenderTreeFrame(sequence, textContent: textContent);

        internal static RenderTreeFrame Attribute(int sequence, string name, UIEventHandler value)
             => new RenderTreeFrame(sequence, attributeName: name, attributeValue: value);

        internal static RenderTreeFrame Attribute(int sequence, string name, object value)
            => new RenderTreeFrame(sequence, attributeName: name, attributeValue: value);

        internal static RenderTreeFrame ChildComponent<T>(int sequence) where T : IComponent
            => new RenderTreeFrame(sequence, typeof(T), 0);

        internal RenderTreeFrame WithElementSubtreeLength(int elementSubtreeLength)
            => new RenderTreeFrame(Sequence, elementName: ElementName, elementSubtreeLength: elementSubtreeLength);

        internal RenderTreeFrame WithComponentSubtreeLength(int componentSubtreeLength)
            => new RenderTreeFrame(Sequence, componentType: ComponentType, componentSubtreeLength: componentSubtreeLength);

        internal RenderTreeFrame WithAttributeSequence(int sequence)
            => new RenderTreeFrame(sequence, attributeName: AttributeName, attributeValue: AttributeValue);

        internal RenderTreeFrame WithComponentInstance(int componentId, IComponent component)
            => new RenderTreeFrame(Sequence, ComponentType, ComponentSubtreeLength, componentId, component);
    }
}
