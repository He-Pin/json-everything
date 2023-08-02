﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `contains`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[Vocabulary(Vocabularies.ApplicatorNextId)]
[DependsOnAnnotationsFrom(typeof(MinContainsKeyword))]
[DependsOnAnnotationsFrom(typeof(MaxContainsKeyword))]
[JsonConverter(typeof(ContainsKeywordJsonConverter))]
public class ContainsKeyword : IJsonSchemaKeyword, ISchemaContainer
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "contains";

	/// <summary>
	/// The schema to match.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="ContainsKeyword"/>.
	/// </summary>
	/// <param name="value">The schema to match.</param>
	public ContainsKeyword(JsonSchema value)
	{
		Schema = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Builds a constraint object for a keyword.
	/// </summary>
	/// <param name="schemaConstraint">The <see cref="SchemaConstraint"/> for the schema object that houses this keyword.</param>
	/// <param name="localConstraints">
	/// The set of other <see cref="KeywordConstraint"/>s that have been processed prior to this one.
	/// Will contain the constraints for keyword dependencies.
	/// </param>
	/// <param name="context">The <see cref="EvaluationContext"/>.</param>
	/// <returns>A constraint object.</returns>
	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, EvaluationContext context)
	{
		var subschemaConstraint = Schema.GetConstraint(JsonPointer.Create(Name), schemaConstraint.BaseInstanceLocation, JsonPointer.Empty, context);
		subschemaConstraint.InstanceLocator = evaluation =>
		{
			if (evaluation.LocalInstance is JsonArray array)
			{
				if (array.Count == 0) return Array.Empty<JsonPointer>();

				return Enumerable.Range(0, array.Count).Select(x => JsonPointer.Create(x));
			}

			if (evaluation.LocalInstance is JsonObject obj &&
			    context.EvaluatingAs is SpecVersion.Unspecified or >= SpecVersion.DraftNext)
				return obj.Select(x => JsonPointer.Create(x.Key));

			return Array.Empty<JsonPointer>();
		};

		return new KeywordConstraint(Name, Evaluator)
		{
			ChildDependencies = new[] { subschemaConstraint }
		};
	}

	private static void Evaluator(KeywordEvaluation evaluation, EvaluationContext context)
	{
		if (evaluation.LocalInstance is JsonArray)
		{
			uint minimum = 1;
			if (evaluation.Results.TryGetAnnotation(MinContainsKeyword.Name, out var minContainsAnnotation))
				minimum = minContainsAnnotation!.GetValue<uint>();
			uint? maximum = null;
			if (evaluation.Results.TryGetAnnotation(MaxContainsKeyword.Name, out var maxContainsAnnotation))
				maximum = maxContainsAnnotation!.GetValue<uint>();

			var validIndices = evaluation.ChildEvaluations
				.Where(x => x.Results.IsValid)
				.Select(x => int.Parse(x.RelativeInstanceLocation.Segments[0].Value))
				.ToArray();
			evaluation.Results.SetAnnotation(Name, JsonSerializer.SerializeToNode(validIndices));
			
			var actual = validIndices.Length;
			if (actual < minimum)
				evaluation.Results.Fail(Name, ErrorMessages.ContainsTooFew, ("received", actual), ("minimum", minimum));
			else if (actual > maximum)
				evaluation.Results.Fail(Name, ErrorMessages.ContainsTooMany, ("received", actual), ("maximum", maximum));
			return;
		}

		if (evaluation.LocalInstance is JsonObject &&
		    context.EvaluatingAs is SpecVersion.Unspecified or >= SpecVersion.DraftNext)
		{
			uint minimum = 1;
			if (evaluation.Results.TryGetAnnotation(MinContainsKeyword.Name, out var minContainsAnnotation))
				minimum = minContainsAnnotation!.GetValue<uint>();
			uint? maximum = null;
			if (evaluation.Results.TryGetAnnotation(MaxContainsKeyword.Name, out var maxContainsAnnotation))
				maximum = maxContainsAnnotation!.GetValue<uint>();

			var validProperties = evaluation.ChildEvaluations
				.Where(x => x.Results.IsValid)
				.Select(x => x.RelativeInstanceLocation.Segments[0].Value)
				.ToArray();
			evaluation.Results.SetAnnotation(Name, JsonSerializer.SerializeToNode(validProperties));
			
			var actual = validProperties.Length;
			if (actual < minimum)
				evaluation.Results.Fail(Name, ErrorMessages.ContainsTooFew, ("received", actual), ("minimum", minimum));
			else if (actual > maximum)
				evaluation.Results.Fail(Name, ErrorMessages.ContainsTooMany, ("received", actual), ("maximum", maximum));
			return;
		}

		evaluation.MarkAsSkipped();
	}
}

internal class ContainsKeywordJsonConverter : JsonConverter<ContainsKeyword>
{
	public override ContainsKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new ContainsKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, ContainsKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(ContainsKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}

public static partial class ErrorMessages
{
	private static string? _containsTooFew;
	private static string? _containsTooMany;

	/// <summary>
	/// Gets or sets the error message for <see cref="ContainsKeyword"/> when there are too few matching items.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[received]] - the number of matching items provided in the JSON instance
	///   - [[minimum]] - the lower limit specified in the schema
	/// </remarks>
	public static string ContainsTooFew
	{
		get => _containsTooFew ?? Get();
		set => _containsTooFew = value;
	}

	/// <summary>
	/// Gets or sets the error message for <see cref="ContainsKeyword"/> when there are too many matching items.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[received]] - the number of matching items provided in the JSON instance
	///   - [[maximum]] - the upper limit specified in the schema
	/// </remarks>
	public static string ContainsTooMany
	{
		get => _containsTooMany ?? Get();
		set => _containsTooMany = value;
	}
}