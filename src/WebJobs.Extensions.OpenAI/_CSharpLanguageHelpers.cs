﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file defines several classes and methods that exist in .NET Core but not in .NET Standard.
// They are defined here to enable certain C# features that otherwise require higher framework versions.
// Redefining types in this way is a standard practice for libary authors that are forced to target .NET Standard.
#if NETSTANDARD

#nullable enable

using System.ComponentModel;

// Copied from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs#L69
namespace System.Diagnostics.CodeAnalysis
{
    sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter will not be null.
        /// </param>
        public NotNullWhenAttribute(bool returnValue) => this.ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// This dummy class is required to compile records when targeting .NET Standard
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    static class IsExternalInit { }
}
#endif