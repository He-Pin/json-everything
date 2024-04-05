﻿using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FunctionalJsonSchema;

public class DescriptionKeywordHandler : IKeywordHandler
{
	public static DescriptionKeywordHandler Instance { get; } = new();

	public string Name => "description";
	public string[]? Dependencies { get; }

	private DescriptionKeywordHandler() { }

	public KeywordEvaluation Handle(JsonNode? keywordValue, EvaluationContext context, IReadOnlyCollection<KeywordEvaluation> siblingEvaluations)
	{
		return KeywordEvaluation.Annotate;
	}

	JsonNode?[] IKeywordHandler.GetSubschemas(JsonNode? keywordValue) => [];
}