﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>A <see cref="JsonDocument"/> representation of the JSON value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static JsonElement SerializeToElement<TValue>(TValue value, JsonSerializerOptions? options = null) =>
            WriteElement(value, GetRuntimeType(value), options);

        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>A <see cref="JsonDocument"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static JsonElement SerializeToElement(object? value, Type inputType, JsonSerializerOptions? options = null) =>
            WriteElement(
                value,
                GetRuntimeTypeAndValidateInputType(value, inputType),
                options);

        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>A <see cref="JsonDocument"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static JsonElement SerializeToElement<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return WriteElement(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>A <see cref="JsonDocument"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/> or its serializable members.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method of the provided
        /// <paramref name="context"/> returns <see langword="null"/> for the type to convert.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        public static JsonElement SerializeToElement(object? value, Type inputType, JsonSerializerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Type runtimeType = GetRuntimeTypeAndValidateInputType(value, inputType);
            return WriteElement(value, GetTypeInfo(context, runtimeType));
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static JsonElement WriteElement<TValue>(in TValue value, Type runtimeType, JsonSerializerOptions? options)
        {
            JsonTypeInfo typeInfo = GetTypeInfo(runtimeType, options);
            return WriteElement(value, typeInfo);
        }

        private static JsonElement WriteElement<TValue>(in TValue value, JsonTypeInfo jsonTypeInfo)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            Debug.Assert(options != null);

            // For performance, share the same buffer across serialization and deserialization.
            using var output = new PooledByteBufferWriter(options.DefaultBufferSize);
            using (var writer = new Utf8JsonWriter(output, options.GetWriterOptions()))
            {
                WriteUsingMetadata(writer, value, jsonTypeInfo);
            }

            return JsonElement.ParseValue(output.WrittenMemory.Span, options.GetDocumentOptions());
        }
    }
}