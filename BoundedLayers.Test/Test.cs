// The MIT License (MIT)
// 
// Copyright (c) 2015 Patricio Zavolinsky
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
using NUnit.Framework;
using System;
using BoundedLayers.Models;
using System.Reflection;
using System.IO;

namespace BoundedLayers.Test
{
	[TestFixture]
	public class Test
	{
		//
		// +-------------+       +-------------+
		// | Shared.Core | <---- | Shared.Host |
		// +-------------+       +-------------+
		//        ^   ^                 ^
		//        |   +-------------+   |
		//        |                 |   |
		// +-------------+       +-------------+
		// |  App.Core   | <---- |  App.Host   |
		// +-------------+       +-------------+
		//
		public IConfiguration GetDefaultLayout()
		{
			return Layers.Configure()
				.Layer("Shared").HasNoReferences()
				.Layer("App").References("Shared")
				.Component("Core").HasNoReferences()
				.Component("Host").References("Core");
		}

		public void AssertDefaultLayout(Solution solution)
		{
			GetDefaultLayout()
				.Validate(solution)
				.AssertThrowsFirst();
		}

		[Test]
		public void NoReferences()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core |       | Shared.Host |
			// +-------------+       +-------------+
			//
			//
			//
			// +-------------+       +-------------+
			// |  App.Core   |       |  App.Host   |
			// +-------------+       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var sharedHost = CreateProject("Shared.Host");
			var appCore    = CreateProject("App.Core"   );
			var appHost    = CreateProject("App.Host"   );

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost
			});

			AssertDefaultLayout(solution);
		}

		[Test]
		public void IntraLayerReferences()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core | <---- | Shared.Host |
			// +-------------+       +-------------+
			//
			//
			//
			// +-------------+       +-------------+
			// |  App.Core   | <---- |  App.Host   |
			// +-------------+       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var sharedHost = CreateProject("Shared.Host", sharedCore.Id);
			var appCore    = CreateProject("App.Core"   );
			var appHost    = CreateProject("App.Host"   , appCore.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost
			});

			AssertDefaultLayout(solution);
		}

		[Test]
		public void InterLayerReferences()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core |       | Shared.Host |
			// +-------------+       +-------------+
			//        ^                     ^
			//        |                     |
			//        |                     |
			// +-------------+       +-------------+
			// |  App.Core   |       |  App.Host   |
			// +-------------+       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var sharedHost = CreateProject("Shared.Host");
			var appCore    = CreateProject("App.Core"   , sharedCore.Id);
			var appHost    = CreateProject("App.Host"   , sharedHost.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost
			});

			AssertDefaultLayout(solution);
		}

		[Test]
		public void AllTogether()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core | <---- | Shared.Host |
			// +-------------+       +-------------+
			//        ^   ^                 ^
			//        |   +-------------+   |
			//        |                 |   |
			// +-------------+       +-------------+
			// |  App.Core   | <---- |  App.Host   |
			// +-------------+       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var sharedHost = CreateProject("Shared.Host", sharedCore.Id);
			var appCore    = CreateProject("App.Core"   , sharedCore.Id);
			var appHost    = CreateProject("App.Host"   , sharedCore.Id, sharedHost.Id, appCore.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost
			});

			AssertDefaultLayout(solution);
		}

		[Test]
		public void ReferenceAnything()
		{
			//
			// +-------------+
			// | Shared.Core |
			// +-------------+
			//        ^
			//        +----------*----------+
			//                              |
			//                       +-------------+
			//                       |  App.Test   |
			//                       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var appTest    = CreateProject("App.Test"   , sharedCore.Id);

			var solution = new Solution(new Project[] {
				sharedCore,
				appTest
			});

			Layers.Configure()
				.Layer("Shared").HasNoReferences()
				.Layer("App").References("Shared")
				.Component("Core").HasNoReferences()
				.Component("Test").ReferencesAnything()
				.Validate(solution)
				.AssertThrowsFirst();
		}

		[Test]
		public void UnknownLayer()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core | <---- | Shared.Host |
			// +-------------+       +-------------+
			//        ^   ^                 ^
			//        |   +-------------+   |
			//        |                 |   |
			// +-------------+       +-------------+
			// |  App.Core   | <---- |  App.Host   |
			// +-------------+       +-------------+
			//        ^
			//        x
			//        |
			// +-------------+
			// |  Unk.Core   |
			// +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var sharedHost = CreateProject("Shared.Host", sharedCore.Id);
			var appCore    = CreateProject("App.Core"   , sharedCore.Id);
			var appHost    = CreateProject("App.Host"   , sharedCore.Id, sharedHost.Id, appCore.Id);
			var unkCore    = CreateProject("Unk.Core"   , appCore.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost,
				unkCore
			});

			var e = Assert.Throws<UnknownLayerException>(() => AssertDefaultLayout(solution));
			Assert.AreEqual(unkCore.Name, e.Project);
		}

		[Test]
		public void UnknownComponent()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core | <---- | Shared.Host |
			// +-------------+       +-------------+
			//        ^   ^                 ^
			//        |   +-------------+   |
			//        |                 |   |
			// +-------------+       +-------------+       +-------------+
			// |  App.Core   | <---- |  App.Host   | <-x-- |  App.Test   |
			// +-------------+       +-------------+       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var sharedHost = CreateProject("Shared.Host", sharedCore.Id);
			var appCore = CreateProject("App.Core", sharedCore.Id);
			var appHost = CreateProject("App.Host", sharedCore.Id, sharedHost.Id, appCore.Id);
			var appTest = CreateProject("App.Test", appHost.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost, appTest
			});

			var e = Assert.Throws<UnknownComponentException>(() => AssertDefaultLayout(solution));
			Assert.AreEqual(appTest.Name, e.Project);
		}

		[Test]
		public void LayerViolation()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core | <---- | Shared.Host |
			// +-------------+       +-------------+
			//        ^                     |
			//        |                     x
			//        |                     v
			// +-------------+       +-------------+
			// |  App.Core   | <---- |  App.Host   |
			// +-------------+       +-------------+
			//
			var sharedCore = CreateProject("Shared.Core");
			var appCore = CreateProject("App.Core", sharedCore.Id);
			var appHost = CreateProject("App.Host", appCore.Id);
			var sharedHost = CreateProject("Shared.Host", sharedCore.Id, appHost.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost
			});

			var e = Assert.Throws<LayerViolationException>(() => AssertDefaultLayout(solution));
			Assert.AreEqual(sharedHost.Name, e.Project);
			Assert.AreEqual(appHost.Name, e.Referenced);
		}

		[Test]
		public void ComponentViolation()
		{
			//
			// +-------------+       +-------------+
			// | Shared.Core | --x-> | Shared.Host |
			// +-------------+       +-------------+
			//        ^                     ^
			//        |                     |
			//        |                     |
			// +-------------+       +-------------+
			// |  App.Core   | <---- |  App.Host   |
			// +-------------+       +-------------+
			//
			var sharedHost = CreateProject("Shared.Host");
			var sharedCore = CreateProject("Shared.Core", sharedHost.Id);
			var appCore = CreateProject("App.Core", sharedCore.Id);
			var appHost = CreateProject("App.Host", appCore.Id, sharedHost.Id);

			var solution = new Solution(new Project[] {
				sharedCore, sharedHost,
				appCore, appHost
			});

			var e = Assert.Throws<ComponentViolationException>(() => AssertDefaultLayout(solution));
			Assert.AreEqual(sharedCore.Name, e.Project);
			Assert.AreEqual(sharedHost.Name, e.Referenced);
		}

		[Test]
		public void Examples()
		{
			//
			// +-------------+        +-------------+
			// | Shared.Core | <--A-- | Shared.Host |
			// +-------------+        +-------------+
			//        ^   ^                  ^
			//        B   +-------C------+   D
			//        |                  |   |
			// +-------------+        +-------------+
			// |  App.Core   | <--E-- |  App.Host   |
			// +-------------+        +-------------+
			//
			GetDefaultLayout()

				// Positive examples
				.ForExample("My.Shared.Host").CanReference("My.Shared.Core") // A
				.ForExample("My.App.Core").CanReference("My.Shared.Core") // B
				.ForExample("My.App.Host").CanReference("My.Shared.Core", "My.Shared.Host", "My.App.Core") // B, C, D

				// Negative examples
				.ForExample("My.Shared.Core").CannotReference("My.Shared.Host", "My.App.Core", "My.App.Host") // ~A, ~B, ~C
				.ForExample("My.Shared.Host").CannotReference("My.App.Host", "My.App.Core") // ~D, ~F (not drawn)
				.ForExample("My.App.Core").CannotReference("My.App.Host", "My.Shared.Host") // ~E, ~G (not drawn)

				;
		}

		[Test]
		public void FailedPositiveExample()
		{
			var config = GetDefaultLayout();
			
			var e = Assert.Throws<LayerViolationException>(() => config.ForExample("My.Shared.Host").CanReference("My.App.Host"));
			Assert.AreEqual("My.Shared.Host", e.Project);
			Assert.AreEqual("My.App.Host", e.Referenced);
		}

		[Test]
		public void FailedNegativeExample()
		{
			var config = GetDefaultLayout();

			var e = Assert.Throws<NegativeExampleAssertionException>(() => config.ForExample("My.App.Host").CannotReference("My.Shared.Core"));
			Assert.AreEqual("My.App.Host", e.Project);
			Assert.AreEqual("My.Shared.Core", e.Referenced);
			Assert.AreEqual("{'App' can reference ['Shared']}", e.LayerRule);
			Assert.AreEqual("{'Host' can reference ['Core']}", e.ComponentRule);
		}


		[Test]
		public void RegexExpressions()
		{
			var sharedCore = CreateProject("Shared");
			var sharedHost = CreateProject("Shared.Host", sharedCore.Id);

			Layers.Configure(Expression.Type.RegularExpression)
				.Layer(@"Shared.*").HasNoReferences()
				.Component(@"Shared").HasNoReferences()
				.Component(@".*\.Host").References(@"Shared")
				.Validate(new Solution(new Project[] { sharedCore, sharedHost }))
				.AssertThrowsFirst();
		}

		[Test]
		public void RegexPrefixExpressions()
		{
			var sharedCore = CreateProject("Shared");
			var sharedHost = CreateProject("App.Host", sharedCore.Id);

			Layers.Configure()
				.Layer("Shared").HasNoReferences()
				.Layer("App").References("r:Sh.*")
				.Component("Shared").HasNoReferences()
				.Component("Host").References("Shared")
				.Validate(new Solution(new Project[] { sharedCore, sharedHost }))
				.AssertThrowsFirst();
		}

		[Test]
		public void AssertionsPassIfNoErrors()
		{
			var noErrors = new ProjectException[0];
			noErrors.Assert();
			noErrors.Assert(s => new Exception(s));
			noErrors.AssertThrowsFirst();
		}

		[Test]
		public void AssertionsFailIfErrors()
		{
			var first  = new UnknownComponentException("First");
			var second = new UnknownLayerException("Second");
			var errors = new ProjectException[] { first, second };

			Assert.Throws<Exception>(() => errors.Assert());
			Assert.Throws<AssertionException>(() => errors.Assert(s => new AssertionException(s)));
			var e = Assert.Throws<UnknownComponentException>(() => errors.AssertThrowsFirst());
			Assert.AreEqual(first, e);
		}

		[Test]
		public void ValidateInvalidSolution()
		{
			Assert.Throws<FileNotFoundException>(() => Layers.Configure().Validate("invalid.sln").Assert());
		}


		protected Project CreateProject(string name, params string[] referenced)
		{
			return new Project(name, name, referenced);
		}
	}
}

