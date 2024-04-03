﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace FunctionalJsonSchema;

public class AllOfKeywordHandler : IKeywordHandler
{
	public string Name => "allOf";
	public string[]? Dependencies { get; }

	public KeywordEvaluation Handle(JsonNode? keywordValue, EvaluationContext context, IReadOnlyList<KeywordEvaluation> evaluations)
	{
		if (keywordValue is not JsonArray constraints)
			throw new SchemaValidationException("'allOf' keyword must contain an array of schemas", context);

		var results = constraints.Select((x, i) =>
		{
			var localContext = context;
			localContext.EvaluationPath = localContext.EvaluationPath.Combine(Name, i);
			localContext.SchemaLocation = localContext.SchemaLocation.Combine(Name, i);

			return localContext.Evaluate(x);
		}).ToArray();

		return new KeywordEvaluation
		{
			Valid = results.All(x => x.Valid),
			Children = [.. results]
		};
	}
}