//
// AssignCultureTest.cs
//
// Author:
//   Ankit Jain (jankit@novell.com)
//
// Copyright 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Microsoft.Build.BuildEngine;

namespace MonoTests.Microsoft.Build.Tasks
{
	[TestFixture]
	public class AssignCultureTest
	{
		static OsType OS;
		static char DSC = Path.DirectorySeparatorChar;

		string [] files;
		Project project;

		[SetUp]
		public void SetUp ()
		{
			if ('/' == DSC) {
				OS = OsType.Unix;
			} else if ('\\' == DSC) {
				OS = OsType.Windows;
			} else {
				OS = OsType.Mac;
				//FIXME: For Mac. figure this out when we need it
			}

			files = new string [] {
				//resx files
				".\\foo.resx", @"bar\foo.resx", 
				"foo.fr.resx", @"dir\abc.en.resx", "foo.bar.resx",
				//non-resx
				"sample.txt", @"bar\sample.txt",
				"sample.it.png", @"dir\sample.en.png", "sample.inv.txt"};

			Engine engine = new Engine (Consts.BinPath);
			project = engine.CreateNewProject ();
		}

		[Test]
		public void TestAssignedFiles ()
		{
			LoadAndBuildProject (files);

			//AssignedFiles
			if (OS == OsType.Unix) {
				CheckItems (new string [] {"./foo.resx", "bar/foo.resx", "foo.fr.resx", "dir/abc.en.resx", "foo.bar.resx",
					"sample.txt", "bar/sample.txt", "sample.it.png", "dir/sample.en.png", "sample.inv.txt"},
					"AssignedFiles", "A2");
			} else if (OS == OsType.Windows) {
				CheckItems (new string [] {".\\foo.resx", @"bar\foo.resx", "foo.fr.resx", @"dir\abc.en.resx", "foo.bar.resx",
					"sample.txt", @"bar\sample.txt", "sample.it.png", @"dir\sample.en.png", "sample.inv.txt"},
					"AssignedFiles", "A2");
			}
		}

		[Test]
		public void TestAssignedFilesWithCulture ()
		{
			LoadAndBuildProject (files);

			//AssignedFilesWithCulture
			if (OS == OsType.Unix) {
				CheckItems (new string [] { "foo.fr.resx", "dir/abc.en.resx", "sample.it.png", "dir/sample.en.png" },
					"AssignedFilesWithCulture", "A2");
			} else if (OS == OsType.Windows) {
				CheckItems (new string [] { "foo.fr.resx", @"dir\abc.en.resx", "sample.it.png", @"dir\sample.en.png" },
					"AssignedFilesWithCulture", "A2");
			}
		}

		[Test]
		public void TestAssignedFilesWithNoCulture ()
		{
			LoadAndBuildProject (files);

			//AssignedFilesWithNoCulture
			if (OS == OsType.Unix) {
				CheckItems (new string [] { "./foo.resx", "bar/foo.resx", "foo.bar.resx", "sample.txt", "bar/sample.txt", "sample.inv.txt"},
					"AssignedFilesWithNoCulture", "A2");
			} else if (OS == OsType.Windows) {
				CheckItems (new string [] { ".\\foo.resx", @"bar\foo.resx", "foo.bar.resx", "sample.txt", @"bar\sample.txt", "sample.inv.txt"},
					"AssignedFilesWithNoCulture", "A2");
			}
		}

		[Test]
		public void TestCultureNeutralAssignedFiles ()
		{
			LoadAndBuildProject (files);

			//CultureNeutralAssignedFiles
			if (OS == OsType.Unix) {
				CheckItems (new string [] { "./foo.resx", "bar/foo.resx", "foo.resx", "dir/abc.resx", "foo.bar.resx",
					"sample.txt", "bar/sample.txt", "sample.png", "dir/sample.png", "sample.inv.txt"},
					"CultureNeutralAssignedFiles", "A2");
			} else if (OS == OsType.Windows) {
				CheckItems (new string [] { ".\\foo.resx", @"bar\foo.resx", "foo.resx", @"dir\abc.resx", "foo.bar.resx",
					"sample.txt", @"bar\sample.txt", "sample.png", @"dir\sample.png", "sample.inv.txt"},
					"CultureNeutralAssignedFiles", "A2");
			}
		}

		void LoadAndBuildProject (string [] files_list)
		{
			string projectText = CreateProjectString (files_list);
			project.LoadXml (projectText);
			Assert.IsTrue (project.Build ("1"), "A1 : Error in building");
		}

		void CheckItems (string [] values, string itemlist_name, string prefix)
		{
			BuildItemGroup group = project.GetEvaluatedItemsByName (itemlist_name);
			Assert.AreEqual (values.Length, group.Count, prefix + "#1");
			for (int i = 0; i < values.Length; i++) {
				Assert.AreEqual (values [i], group [i].FinalItemSpec, prefix + "#2");
				Assert.IsTrue (group [i].HasMetadata ("Child"), prefix + "#3");
				Assert.AreEqual ("ChildValue", group [i].GetMetadata ("Child"), prefix + "#4");
			}
		}

		string CreateProjectString (string [] files)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003""><ItemGroup>");
			foreach (string file in files)
				sb.AppendFormat ("<Files Include=\"{0}\"><Child>ChildValue</Child></Files>\n", file);

			sb.Append (@"</ItemGroup>
			<Target Name=""1"">
				<AssignCulture Files=""@(Files)"" >
					<Output TaskParameter=""AssignedFiles"" ItemName=""AssignedFiles"" />
					<Output TaskParameter=""AssignedFilesWithCulture"" ItemName=""AssignedFilesWithCulture"" />
					<Output TaskParameter=""AssignedFilesWithNoCulture"" ItemName=""AssignedFilesWithNoCulture"" />
					<Output TaskParameter=""CultureNeutralAssignedFiles"" ItemName=""CultureNeutralAssignedFiles"" />
				</AssignCulture>
			</Target>
		</Project>");

			return sb.ToString ();
		}
	}
}
