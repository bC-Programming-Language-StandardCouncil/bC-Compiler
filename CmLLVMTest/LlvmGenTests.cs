﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cmc;
using Cmc.Core;
using Cmc.Decl;
using Cmc.Expr;
using Cmc.Stmt;
using CmLLVM;
using LLVMSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CmLLVMTest
{
	[TestClass]
	public class LlvmItselfTests
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int Add(int a, int b);

		[TestMethod]
		public void Exmaple()
		{
			var success = new LLVMBool(0);
			var moduleRef = LLVM.ModuleCreateWithName("LLVMSharpIntro");

			var sum = LLVM.AddFunction(moduleRef, "sum", LLVM.FunctionType(LLVM.Int32Type(), new[] {LLVM.Int32Type(), LLVM.Int32Type()}, false));

			var builder = LLVM.CreateBuilder();
			LLVM.PositionBuilderAtEnd(builder, LLVM.AppendBasicBlock(sum, "entry"));
			LLVM.BuildRet(builder, LLVM.BuildAdd(builder, LLVM.GetParam(sum, 0), LLVM.GetParam(sum, 1), "tmp"));

			if (LLVM.VerifyModule(moduleRef, LLVMVerifierFailureAction.LLVMPrintMessageAction, out var error) != success)
				Console.WriteLine($"Error: {error}");

			LLVM.LinkInMCJIT();

			LLVM.InitializeX86TargetMC();
			LLVM.InitializeX86Target();
			LLVM.InitializeX86TargetInfo();
			LLVM.InitializeX86AsmParser();
			LLVM.InitializeX86AsmPrinter();

			var options = new LLVMMCJITCompilerOptions {NoFramePointerElim = 1};
			LLVM.InitializeMCJITCompilerOptions(options);
			if (LLVM.CreateMCJITCompilerForModule(out var engine, moduleRef, options, out error) != success)
				Console.WriteLine($"Error: {error}");

			var addMethod = (Add) Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(engine, sum), typeof(Add));
			var result = addMethod(10, 10);

			Console.WriteLine("Result of sum is: " + result);

			LLVM.DumpModule(moduleRef);
			LLVM.DumpValue(sum);

			LLVM.DisposeBuilder(builder);
			LLVM.DisposeExecutionEngine(engine);
		}
	}

	[TestClass]
	public class LlvmGenTests
	{
		/// <summary>
		///  id function
		///  let id = { a: i8 -> a }
		/// </summary>
		private static VariableDeclaration IdDeclaration =>
			new VariableDeclaration(MetaData.Empty, "id",
				new LambdaExpression(MetaData.Empty,
					new StatementList(MetaData.Empty),
					new List<VariableDeclaration>(new[]
					{
						new VariableDeclaration(MetaData.Empty, "a", type:
							new UnknownType(MetaData.Empty, "i8"))
					})));

		// [TestMethod]
		public void LlvmGenTest1()
		{
			var res = Gen.Generate("my module",
				new VariableDeclaration(MetaData.Empty,
					"i", new IntLiteralExpression(MetaData.Empty, "1", true)),
				new VariableDeclaration(MetaData.Empty,
					"j", new StringLiteralExpression(MetaData.Empty, "boy next door")),
				new VariableDeclaration(MetaData.Empty,
					"main", new LambdaExpression(MetaData.Empty,
						new StatementList(MetaData.Empty,
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "print"),
									new List<Expression>(new[]
									{
										new VariableExpression(MetaData.Empty, "j")
									}))),
							new ReturnStatement(MetaData.Empty,
								new IntLiteralExpression(MetaData.Empty, "0", true)))))
			);
			Console.WriteLine(res);
		}

		// [TestMethod]
		public void LlvmGenTest2()
		{
			var body = new StatementList(MetaData.Empty,
				new ExpressionStatement(MetaData.Empty,
					new FunctionCallExpression(MetaData.Empty,
						new VariableExpression(MetaData.Empty, "id"),
						new List<Expression>(new[]
						{
							new FunctionCallExpression(MetaData.Empty,
								new VariableExpression(MetaData.Empty, "id"),
								new List<Expression>(new[]
								{
									new FunctionCallExpression(MetaData.Empty,
										new VariableExpression(MetaData.Empty, "id"),
										new List<Expression>(new[]
										{
											new IntLiteralExpression(MetaData.Empty, "123", true, 8)
										})
									)
								}))
						}))));
			var res = Gen.Generate(
				"my module",
				IdDeclaration,
				new VariableDeclaration(MetaData.Empty,
					"main", new LambdaExpression(MetaData.Empty, body)));
			Console.WriteLine(res);
		}


		// [TestMethod]
		public void CodeGenFailTest2() => Assert.ThrowsException<CompilerException>(() =>
			Gen.RunLlvm(
				"my module",
				"out.exe",
				new VariableDeclaration(MetaData.Empty,
					"i", new IntLiteralExpression(MetaData.Empty, "1", true)),
				new VariableDeclaration(MetaData.Empty,
					"j", new StringLiteralExpression(MetaData.Empty, "boy next door")),
				new VariableDeclaration(MetaData.Empty,
					"main", new LambdaExpression(MetaData.Empty,
						new StatementList(MetaData.Empty,
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "print"),
									new List<Expression>(new[]
									{
										new VariableExpression(MetaData.Empty, "local")
									}))),
							new ReturnStatement(MetaData.Empty,
								new IntLiteralExpression(MetaData.Empty, "0", true))
						))),
				new VariableDeclaration(MetaData.Empty,
					"main", new LambdaExpression(MetaData.Empty,
						new StatementList(MetaData.Empty,
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "print"),
									new List<Expression>(new[]
									{
										new VariableExpression(MetaData.Empty, "j")
									}))),
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "myfunc"),
									new List<Expression>())),
							new ReturnStatement(MetaData.Empty,
								new IntLiteralExpression(MetaData.Empty, "0", true)))))
			));

		/// <summary>
		///  ambiguous main definition
		/// </summary>
		// [TestMethod]
		public void CodeGenFailTest1() => Assert.ThrowsException<CompilerException>(() =>
			Gen.RunLlvm(
				"my module",
				"out.exe",
				new VariableDeclaration(MetaData.Empty,
					"i", new IntLiteralExpression(MetaData.Empty, "1", true)),
				new VariableDeclaration(MetaData.Empty,
					"j", new StringLiteralExpression(MetaData.Empty, "boy next door")),
				new VariableDeclaration(MetaData.Empty,
					"main", new LambdaExpression(MetaData.Empty,
						new StatementList(MetaData.Empty,
							new VariableDeclaration(MetaData.Empty,
								"local", new StringLiteralExpression(MetaData.Empty, "NullRefTest")),
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "print"),
									new List<Expression>(new[]
									{
										new VariableExpression(MetaData.Empty, "local")
									}))),
							new ReturnStatement(MetaData.Empty,
								new IntLiteralExpression(MetaData.Empty, "0", true))
						))),
				new VariableDeclaration(MetaData.Empty,
					"main", new LambdaExpression(MetaData.Empty,
						new StatementList(MetaData.Empty,
							new VariableDeclaration(MetaData.Empty,
								"j", new StringLiteralExpression(MetaData.Empty, "Hello, World")),
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "print"),
									new List<Expression>(new[]
									{
										new VariableExpression(MetaData.Empty, "j")
									}))),
							new ExpressionStatement(MetaData.Empty,
								new FunctionCallExpression(MetaData.Empty,
									new VariableExpression(MetaData.Empty, "myfunc"),
									new List<Expression>())),
							new ReturnStatement(MetaData.Empty,
								new IntLiteralExpression(MetaData.Empty, "0", true)))))
			));

		[TestInitialize]
		public void Init() => Errors.ErrList.Clear();
	}
}