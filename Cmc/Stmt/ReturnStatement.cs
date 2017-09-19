﻿using System.Collections.Generic;
using System.Linq;
using Cmc.Core;
using Cmc.Decl;
using Cmc.Expr;
using JetBrains.Annotations;

namespace Cmc.Stmt
{
	/// <summary>
	///  when codegen, use `converted statement`
	/// </summary>
	public class ReturnStatement : ExpressionStatement
	{
		public ReturnLabelDeclaration ReturnLabel;
		[CanBeNull] private readonly string _labelName;

		public ReturnStatement(
			MetaData metaData,
			[CanBeNull] Expression expression = null,
			[CanBeNull] string labelName = null) :
			base(metaData, expression ?? new NullExpression(metaData))
		{
			_labelName = labelName;
		}

		public override void SurroundWith(Environment environment)
		{
			// base.SurroundWith(environment);
			Env = environment;
			Expression.SurroundWith(Env);
			var returnLabel = Env.FindReturnLabelByName(_labelName ?? "");
			if (null == returnLabel)
				Errors.AddAndThrow($"{MetaData.GetErrorHeader()}cannot return outside a lambda");
			ReturnLabel = returnLabel;
			ReturnLabel.StatementsUsingThis.Add(this);
			if (Expression is AtomicExpression) return;
			var variableName = $"{MetaData.TrimedFileName}{MetaData.LineNumber}{GetHashCode()}";
			ConvertedStatementList = new StatementList(MetaData,
				new VariableDeclaration(MetaData, variableName, Expression, type: Expression.GetExpressionType()),
				new ReturnStatement(MetaData, new VariableExpression(MetaData, variableName), _labelName)
				{
					ReturnLabel = ReturnLabel
				});
		}

		/// <summary>
		///   make this an inlined return statement
		/// </summary>
		/// <param name="returnValueStorer">the variable used to store the return value</param>
		public void InlineThis(VariableExpression returnValueStorer)
		{
			if (null != ConvertedStatementList)
			{
				var varDecl = ConvertedStatementList.Statements.First();
				var varExpr = (VariableExpression) ((ReturnStatement) ConvertedStatementList.Statements.Last()).Expression;
				ConvertedStatementList = new StatementList(MetaData,
					varDecl,
					new AssignmentStatement(MetaData, returnValueStorer, varExpr));
			}
			else
				ConvertedStatementList = new StatementList(MetaData,
					new AssignmentStatement(MetaData, returnValueStorer, Expression));
		}

		public override IEnumerable<string> Dump() => new[]
			{
				$"return statement [{ReturnLabel}]:\n"
			}
			.Concat(Expression.Dump().Select(MapFunc));
	}
}