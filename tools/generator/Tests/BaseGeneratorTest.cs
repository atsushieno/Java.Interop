﻿using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using Xamarin.Android.Binder;
using System.Collections.Generic;

namespace generatortests
{
	public class BaseGeneratorTest
	{
		StringWriter sw = null;

		[SetUp]
		public void Setup ()
		{
			Options = new CodeGeneratorOptions ();
			Options.ApiLevel = "4";
			Options.GlobalTypeNames = true;
			Options.EnumFieldsMapFile = null;
			Options.EnumMethodsMapFile = null;
			Options.AssemblyQualifiedName = "Mono.Android, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
			Options.OnlyBindPublicTypes = true;
			sw = new StringWriter ();
			AdditionalSourceDirectories = new List<string> ();
		}

		protected CodeGeneratorOptions Options = null;
		protected Assembly BuiltAssembly = null;
		protected List<string> AdditionalSourceDirectories;

		public void Execute ()
		{
			CodeGenerator.Run (Options);
			var output = sw.ToString ();
			if (output.Contains ("error")) {
				Assert.Fail (output);
			}
			string[] errors;
			BuiltAssembly = Compiler.Compile (Options, "Mono.Andoroid",
				AdditionalSourceDirectories, out errors);
			Assert.AreEqual (0, errors.Length, string.Join (Environment.NewLine, errors));
			Assert.IsNotNull (BuiltAssembly);
		}

		protected void CompareOutputs (string sourceDir, string destinationDir)
		{
			var files = Directory.GetFiles (sourceDir);
			foreach (var file in files) {
				if (Path.GetExtension (file) == ".xml")
					continue;
				var filename = Path.GetFileName (file);
				var dest = Path.Combine (destinationDir, filename);
				if (!File.Exists (dest)) {
					Assert.Fail (string.Format ("Expected {0} but it was not generated.", dest));
				} else if (!FileCompare (file, dest)) {
					Assert.Fail (string.Format ("The Files {0} and {1} do not match", file, dest));
				}
			}
		}

		protected void Cleanup (string path)
		{
			if (Directory.Exists (path))
				Directory.Delete (path, true);
		}

		protected bool FileCompare (string file1, string file2)
		{
			bool result = false;

			result = File.Exists (file1) && File.Exists (file2);

			if (result) {
				byte[] f1 = File.ReadAllBytes (file1);
				byte[] f2 = File.ReadAllBytes (file2);

				var hash = MD5.Create ();
				var f1hash = Convert.ToBase64String (hash.ComputeHash (f1));
				var f2hash = Convert.ToBase64String (hash.ComputeHash (f2));
				result = f1hash.Equals (f2hash);
			}

			return result;
		}

		protected void RunAllTargets (string outputRelativePath, string apiDescriptionFile, string expectedRelativePath, string[] additionalSupportPaths = null)
		{
			Run (CodeGenerationTarget.XamarinAndroid,   Path.Combine ("out", outputRelativePath),       apiDescriptionFile,     Path.Combine ("expected", expectedRelativePath),        additionalSupportPaths);
			Run (CodeGenerationTarget.JavaInterop1,     Path.Combine ("out.ji", outputRelativePath),    apiDescriptionFile,     Path.Combine ("expected.ji", expectedRelativePath),     additionalSupportPaths);
		}

		protected void Run (CodeGenerationTarget target, string outputPath, string apiDescriptionFile, string expectedPath, string[] additionalSupportPaths = null)
		{
			Cleanup (outputPath);

			Options.CodeGenerationTarget                        = target;
			Options.ApiDescriptionFile                          = apiDescriptionFile;
			Options.ManagedCallableWrapperSourceOutputDirectory = outputPath;

			if (additionalSupportPaths != null)
				AdditionalSourceDirectories.AddRange (additionalSupportPaths);

			Execute ();

			CompareOutputs (expectedPath, outputPath);
		}
	}
}
