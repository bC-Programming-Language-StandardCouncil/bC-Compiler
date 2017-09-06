﻿using System;
using System.Linq;
using System.Text;
using Cmc;
using Cmc.Core;
using Cmc.Expr;
using JetBrains.Annotations;
using static System.StringComparison;

namespace LLVM
{
	public static class GenExpression
	{
		public static void StoreAtomicExpression(
			[NotNull] StringBuilder builder,
			[NotNull] AtomicExpression expression,
			ref ulong varName)
		{
			if (expression is IntLiteralExpression integer)
				builder.AppendLine(
					$"  store {integer.Type} {integer.Value},{integer.Type}* %var{varName},align {integer.Type.Align}");
			else if (expression is BoolLiteralExpression boolean)
				builder.AppendLine(
					$"  store i8 {boolean.ValueToInt()},i8* %var{varName},align 1");
			else if (expression is VariableExpression variable)
			{
				var type = variable.GetExpressionType();
				var varAddress = variable.Declaration.Address;
				builder.AppendLine(
					$"  store {type} %var{varAddress},{type}* %var{varAddress},align {type.Align}");
			}
		}

		public static void GenAstExpression(
			[NotNull] StringBuilder builder,
			[NotNull] Expression element,
			ref ulong varName)
		{
			if (element is StringLiteralExpression str)
				builder.AppendLine(
					$"  store i8* getelementptr inbounds ([{str.Length} x i8]," +
					$"[{str.Length} x i8]* @.str{str.ConstantPoolIndex},i32 0,i32 0)," +
					$"i8** %var{varName},align 8");
			else if (element is AtomicExpression expression)
				// maybe I shouldn't simply "store" this value
				StoreAtomicExpression(builder, expression, ref varName);
			else if (element is FunctionCallExpression functionCall)
			{
				// function callee and parameters should already be splitted
				// into atomic expressions
				if (functionCall.Receiver is VariableExpression variable &&
				    string.Equals(variable.Name, "print", Ordinal))
				{
					if (functionCall.ParameterList.Count != 1 ||
					    !Equals(functionCall.ParameterList.First().GetExpressionType(),
						    new PrimaryType(MetaData.BuiltIn, "string")))
						Errors.Add($"{functionCall.MetaData.GetErrorHeader()}error call to function print.");
					var param = functionCall.ParameterList.First();
					param.PrintDumpInfo();
					builder.AppendLine(
						$"  call i32 @puts(i8* %{233})");
				}
				else
				{
					// TODO gen other function calls
				}
			}
		}
	}
}