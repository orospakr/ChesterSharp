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

        public static readonly string TEST_DATABASE = "chestersharp_test";
        public static readonly string CREATION_TEST_DATABASE = "chestersharp_creation_test";

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

            couch.EnsureDatabaseDeleted(TEST_DATABASE);
            couch.EnsureDatabaseDeleted(CREATION_TEST_DATABASE);
            couch.CreateDatabase(TEST_DATABASE);
        }

        [Test]
        public void ShouldJoinNonTrailingUriPathElements() {
            Assert.AreEqual("awesome/win", Couch.UriJoin("awesome", "win"));
        }

        [Test]
        public void ShouldJoinTrailingUriPathElements() {
            Assert.AreEqual("awesome/win", Couch.UriJoin("awesome/", "win"));
        }

        [Test]
        public void ShouldJoinUriPathElementsAgainstBase() {
            Assert.AreEqual("/win", Couch.UriJoin("/", "win"));
        }

        [Test]
        public void ShouldBuildBaseDatabaseUri() {
            var dbUri = couch.BuildDatabaseUri("snively");
            Assert.AreEqual("/snively", dbUri.AbsolutePath);
            Assert.AreEqual("http://localhost:5984/snively", dbUri.ToString());
        }

        [Test]
        public void ShouldBuildDocumentUri() {
            Assert.AreEqual("http://localhost:5984/snively/mynote", couch.BuildDocumentUri("snively", "mynote").ToString());
        }

        [Test]
        public void ShouldGetADocumentFromCouchDb() {
            // var t = couch.getDo
        }

        [Test]
        public void ShouldThrowErrorPutBogusDocumentUpdateToCouchDb() {
            bool gotException = false;
            try {
                var t = couch.PutRawDocumentUpdate("snively", "blah blah", "anidentifier");
                t.Wait();
            } catch (AggregateException ae) {
                ae.Handle( (e) => {
                    if(e is CouchException) {
                        gotException = true;
                        return true;
                    }
                    return false;
                });
            }
            Assert.IsTrue(gotException);
        }

        [Test]
        public void ShouldPutDocumentUpdateToCouchDb() {
            var p = new Person { Name = "Sally Acorn"};
            var t = couch.PutDocumentUpdate<Person>("chestercouch_test", p, "sally");
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
            var t = couch.GetServerVersion();
			t.Wait();
			Console.Out.WriteLine (t.Result);
            Assert.AreEqual("1.2.0", t.Result);
		}

        [Test()]
        public void ShouldGetDocument () {
            var r = couch.GetRawDocument(CREATION_TEST_DATABASE, "d359dcdfbd8f358c8a0207c12700012c");
            r.Wait();
            Console.Out.WriteLine(r.Result);
        }

        [Test]
        public void ShouldCheckForDatabaseExistence() {
            var t = couch.DoesDatabaseExist(TEST_DATABASE);
            t.Wait();
            Assert.IsTrue(t.Result);
        }

        [Test]
        public void ShouldCheckForDatabaseNonExistence() {
            var t = couch.DoesDatabaseExist("snordelsaldfsaf");
            t.Wait();
            Assert.IsFalse(t.Result);
        }

        [Test]
        public void ShouldCreateADatabase() {
            var te = couch.DoesDatabaseExist(CREATION_TEST_DATABASE);
            te.Wait();
            Assert.IsFalse(te.Result, "Creation test database should not exist before creation test.");
            var tc = couch.CreateDatabase(CREATION_TEST_DATABASE);
            tc.Wait();
            te = couch.DoesDatabaseExist(CREATION_TEST_DATABASE);
            te.Wait();
            Assert.IsTrue(te.Result, "Creation test database should exist after creation test.");
        }

        [Test]
        public void ShouldDeleteADatabase() {
            var t = couch.DeleteDatabase(TEST_DATABASE);
            t.Wait();
            var te = couch.DoesDatabaseExist(TEST_DATABASE);
            te.Wait();
            Assert.IsFalse(te.Result);
        }
	}
}
