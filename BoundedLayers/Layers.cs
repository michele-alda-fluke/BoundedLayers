using System;
using System.Collections.Generic;
using System.Linq;
using BoundedLayers.Models;

namespace BoundedLayers
{
	public interface ILayers
	{
		Layers.IRule Layer(string name);
		Layers.IRule Component(string name);
		IEnumerable<ProjectException> Validate(string solutionPath);
		IEnumerable<ProjectException> Validate(Solution solution);
	}

	public class Layers : ILayers
	{
		public static ILayers Configure(Expression.Type expType = Expression.Type.AssemblyPart)
		{
			return new Layers(expType);
		}

		private readonly List<Rule> _layerRules = new List<Rule>();
		private readonly List<Rule> _componentRules = new List<Rule>();
		private readonly Expression.Type _expType;

		public Layers(Expression.Type expType)
		{
			_expType = expType;
		}

		public IRule Layer(string name)
		{
			var rule = new Rule(this, name);
			_layerRules.Add(rule);
			return rule;
		}

		public IRule Component(string name)
		{
			var rule = new Rule(this, name);
			_componentRules.Add(rule);
			return rule;
		}

		public interface IRule
		{
			ILayers References(params string[] names);
			ILayers HasNoReferences();
		}

		public class Rule : IRule
		{
			private readonly Layers _layers;
			private readonly IExpression _nameEx;
			private readonly List<IExpression> _referenced;

			public Rule(Layers layers, string name)
			{
				_layers = layers;
				_nameEx = layers.CreateExpression(name);
				_referenced = new List<IExpression>();
			}

			public ILayers References(params string[] names)
			{
				_referenced.AddRange(names.Select(n => _layers.CreateExpression(n)));
				return _layers;
			}

			public ILayers HasNoReferences()
			{
				return _layers;
			}

			public bool Matches(string name)
			{
				return _nameEx.Matches(name);
			}

			public bool Allows(string name)
			{
				return Matches(name) || _referenced.Any(e => e.Matches(name));
			}
		}

		public IExpression CreateExpression(string s)
		{
			return Expression.Create(_expType, s);
		}

		public IEnumerable<ProjectException> Validate(string solutionPath)
		{
			return Validate(new Solution(solutionPath));
		}

		public IEnumerable<ProjectException> Validate(Solution solution)
		{
			var res = new List<ProjectException>();

			foreach (var project in solution.Projects)
			{
				// verify layer rules
				var layers = _layerRules.Where(r => r.Matches(project.Name)).ToArray();
				var components = _componentRules.Where(r => r.Matches(project.Name)).ToArray();

				if (!layers.Any()) 
				{
					res.Add(new UnknownLayerException(project));
				}
				if (!components.Any()) 
				{
					res.Add(new UnknownComponentException(project));
				}

				foreach (var referenced in project.References.Select(id => solution.Find(id)))
				{
					if (!layers.Any(r => r.Allows(referenced.Name)))
					{
						res.Add(new LayerViolationException(project, referenced));
					}
					if (!components.Any(r => r.Allows(referenced.Name)))
					{
						res.Add(new ComponentViolationException(project, referenced));
					}
				}
			}

			return res;
		}
	}

	public static class ValidationResultExtensions
	{
		public static void Assert(this IEnumerable<ProjectException> res)
		{
			res.Assert(s => new Exception(s));
		}

		public static void Assert<T>(this IEnumerable<ProjectException> res, Func<string, T> ctor) where T : Exception
		{
			if (!res.Any()) return;
			throw ctor(string.Join("\n", res.Select(e => e.Message)));
		}
	}
}