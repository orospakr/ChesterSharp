using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Net.Http;
using Newtonsoft.Json;

namespace SharpCouch
{
    public class Thinger : CouchDocument {
    }

    public class Kase : CouchDocument {
    }

    public class Person: CouchDocument {
        public string Name { get; set; }

        // "type" field expected by the PersonDesign's map.
        [JsonProperty("type")]
        public string DocumentType { get; set; }
    }

    [DesignDocumentName("person")]
    public class PersonDesign : DesignDocument {
        public class All : View {
            public override string Map { get {
                    return @"function(doc) {
                        if(doc[""type""] === ""Person"") {
                            emit(doc[""_id""], doc.name);
                        }
                    }";
                }
            }
        }
    }

	[TestFixture]
	public class CouchTest
	{
        Couch couch;

        public static readonly string TEST_DATABASE = "chestersharp_test";
        public static readonly string CREATION_TEST_DATABASE = "chestersharp_creation_test";

        // these are instantiated in BeforeEach() because they are modified (id and rev updated) by the
        // Couch routines they are subjected to, and this TestFixture object is only instantiated
        // once for all tests.
        private Person SallyFixture;
        private Person HayekFixture;
        public List<Person> PersonFixtures;

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

            SallyFixture = new Person {Name = "Sally Acorn", Id = "sally", DocumentType = "Person"};
            HayekFixture = new Person {Name = "Frederich Hayek", DocumentType = "Person"}; // hayek gets a generated Id

            PersonFixtures = new List<Person>() {
                SallyFixture,
                HayekFixture
            };
            couch.EnsureDatabaseDeleted(TEST_DATABASE).Wait();
            couch.EnsureDatabaseDeleted(CREATION_TEST_DATABASE).Wait();
            couch.CreateDatabase(TEST_DATABASE).Wait();

            foreach (var personFixture in PersonFixtures) {
                couch.CreateDocument<Person>(TEST_DATABASE, personFixture).Wait();
            }
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
            var dbUri = couch.BuildDatabaseUri("foo");
            Assert.AreEqual("/foo", dbUri.AbsolutePath);
            Assert.AreEqual("http://localhost:5984/foo", dbUri.ToString());
        }

        [Test]
        public void ShouldBuildDocumentUri() {
            Assert.AreEqual("http://localhost:5984/foo/mynote", couch.BuildDocumentUri("foo", "mynote").ToString());
        }

        [Test]
        public void ShouldGetADocumentFromCouchDb() {
            // var t = couch.getDo
        }

        [Test]
        public void ShouldThrowErrorPutBogusDocumentUpdateToCouchDb() {
            bool gotException = false;
            try {
                var t = couch.PutRawDocument(TEST_DATABASE, "blah blah", "anidentifier");
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
        public void ShouldUpdateDocument() {
            var newName = "Excellent Economist";
            Assert.NotNull(HayekFixture.Rev);
            HayekFixture.Name = newName;
            var t = couch.UpdateDocument<Person>(TEST_DATABASE, HayekFixture);
            t.Wait();

            var check = couch.GetDocument<Person>(TEST_DATABASE, HayekFixture.Id);
            check.Wait();
            Assert.AreEqual(newName, check.Result.Name);
        }

        [Test]
        public void ShouldRefuseToUpdateDocumentWithoutRev() {
            var gotException = false;
            try {
                var p = new Person { Name = "MissingNo", Id = "missigno"};
                var t = couch.UpdateDocument<Person>(TEST_DATABASE, p);
                t.Wait();
            } catch (AggregateException ae) {
                ae.Handle((e) => {
                    if (e is ArgumentOutOfRangeException) {
                        gotException = true;
                        return true;
                    }
                    return false;
                });
            }
            Assert.IsTrue(gotException);
        }

        /// <summary>
        /// Test of system breakage: at least on Mono 2.11.4, the relative URI appending behaviour
        /// of Uri does not appear to work as seen in the MS documentation.
        /// </summary>
        [Test]
        public void SystemDotUriShouldNotBeADick() {
            var myuri = new Uri("http://blatz.ca");
            Assert.AreEqual("/", myuri.AbsolutePath);
            var myuriWithTopPath = new Uri(myuri, "top");
            Assert.AreEqual("/top", myuriWithTopPath.AbsolutePath);
            var myuriWithSubPath = new Uri(myuriWithTopPath, "down");
            Assert.AreEqual("/top/down", myuriWithSubPath.AbsolutePath);
        }

//        /// <summary>
//        /// Test of system breakage: at least on Mono 2.11.4, HttpClient hangs when trying to post blank.
//        /// The failure case is that this test hangs while waiting for the test to complete!
//        /// </summary>
//        [Test]
//        public void SystemNetHttpClientShouldNotHangWhenPostingEmptyString() {
//            // THEN CONTINUE AND CREATE PPOST NEW DOCUMENT CODE in Couch for fixture pushing
//            var http = new HttpClient();
//            var emptyString = new StringContent("");
//            var t = http.PostAsync(new Uri("http://www.debian.org/"), emptyString);
//            t.Wait();
//        }

		[Test]
		public void ShouldGetCouchDbVersionNumber () {
            var t = couch.GetServerVersion();
			t.Wait();
			Console.Out.WriteLine (t.Result);
            Assert.AreEqual("1.2.0", t.Result);
		}

        [Test]
        public void ShouldGetRawDocument () {
            var r = couch.GetRawDocument(TEST_DATABASE, SallyFixture.Id);
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

        [Test]
        public void ShouldCreateDesignDocument() {
            var dd = new PersonDesign();
            // var t = couch.CreateDocument<PersonDesign>(TEST_DATABASE, dd);
            var t = couch.UpdateDesignDocument<PersonDesign>(TEST_DATABASE);
            t.Wait();
        }

        [Test]
        public void ShouldGetViewOfDesignDocument() {
            ShouldCreateDesignDocument();
            var t = couch.GetView<Person>(TEST_DATABASE, DesignDocument.GetDesignDocumentName<PersonDesign>(), "All");
            t.Wait();
            Assert.AreEqual(2, t.Result.Count);
        }

        [Test]
        public void ShouldGetViewOfDesignDocumentWithTypeSafeApi() {
            ShouldCreateDesignDocument();
            var t = couch.GetView<PersonDesign, PersonDesign.All, Person>(TEST_DATABASE);
            t.Wait();
            Assert.AreEqual(2, t.Result.Count);
        }
	}
}
