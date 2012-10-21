using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SharpCouch
{
    public class Thinger : CouchDocument {
    }

    public class Kase : CouchDocument {
    }

    public class Person: CouchDocument {
        public string Name { get; set; }
    }

	[TestFixture]
	public class CouchTest
	{
        Couch couch;

        public static readonly List<Person> PersonFixtures = new List<Person>() {
            new Person {Name = "Sally Acorn"},
            new Person {Name = "Jo Blow"}
        };

//		[Test()]
//		public void TestCase ()
//		{
//			var t = Couch.fetchUrlTestNew();
//			t.Wait();
//			Console.WriteLine (t.Result);
//			Assert.IsTrue(false);
//		}

        [SetUp]
        public void BeforeEach() {
            couch = new Couch("localhost", 5984);

            // use my own routines -- even though I would be using my own code as a testing predicate,
            // bugs would still cause obvious failures, and I would only have to use a small slice of my code


        }

        [Test]
        public void ShouldJoinNonTrailingUriPathElements() {
            Assert.AreEqual("awesome/win", Couch.uriJoin("awesome", "win"));
        }

        [Test]
        public void ShouldJoinTrailingUriPathElements() {
            Assert.AreEqual("awesome/win", Couch.uriJoin("awesome/", "win"));
        }

        [Test]
        public void ShouldJoinUriPathElementsAgainstBase() {
            Assert.AreEqual("/win", Couch.uriJoin("/", "win"));
        }

        [Test]
        public void ShouldBuildBaseDatabaseUri() {
            var dbUri = couch.buildDatabaseUri("snively");
            Assert.AreEqual("/snively", dbUri.AbsolutePath);
            Assert.AreEqual("http://localhost:5984/snively", dbUri.ToString());
        }

        [Test]
        public void ShouldBuildDocumentUri() {
            Assert.AreEqual("http://localhost:5984/snively/mynote", couch.buildDocumentUri("snively", "mynote").ToString());
        }

        [Test]
        public void ShouldGetADocumentFromCouchDb() {
            // var t = couch.getDo
        }

        [Test]
        public void ShouldPostDocumentToCouchDb() {
            var t = couch.postRawDocumentUpdate("snively", "blah blah", "couch db will not apperciate this string");
            t.Wait();
        }

        [Test]
        public void SystemDotUriShouldNotBeADick() {
            var myuri = new Uri("http://blatz.ca");
            Assert.AreEqual("/", myuri.AbsolutePath);
            var myuriWithTopPath = new Uri(myuri, "top");
            Assert.AreEqual("/top", myuriWithTopPath.AbsolutePath);
            var myuriWithSubPath = new Uri(myuriWithTopPath, "down");
            Assert.AreEqual("/top/down", myuriWithSubPath.AbsolutePath);
        }

		[Test()]
		public void ShouldGetCouchDbVersionNumber () {
            var t = couch.getServerVersion();
			t.Wait();
			Console.Out.WriteLine (t.Result);
            Assert.AreEqual("1.2.0", t.Result);
		}

        [Test()]
        public void ShouldGetDocument () {
            var r = couch.getRawDocument("snively", "d359dcdfbd8f358c8a0207c12700012c");
            r.Wait();
            Console.Out.WriteLine(r.Result);
        }

	}
}
