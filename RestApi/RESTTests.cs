namespace RestApi
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using RestSharp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// REST Api tests, include basic negatives.
    /// </summary>
    [Parallelizable(ParallelScope.Children)]
    public class RESTTests
    {
        private RestClient client;
        private string testPrefix = "Autotest_";
        private List<int> CreatedEntitys;
        private RestRequest getRequest;
        private RestRequest postRequest;
        private int retrysToFail = 10;
        private TimeSpan timeBetweenRetrys = TimeSpan.FromSeconds(1);
        private bool forceDbCleanup = true;
        private int forcedDbCleanupRetrys = 30;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            client = new RestClient("https://superhero.qa-test.csssr.com/");
            CreatedEntitys = new List<int>();
        }

        [SetUp]
        public void SetUp()
        {
            getRequest = new RestRequest("/superheroes", Method.GET);
            postRequest = new RestRequest("/superheroes", Method.POST);
        }


        [Test]
        [Order(10)]
        public void Test_10_Get_WhenSendTestresults_ShouldReturnOk()
        {
            //Arrange

            //Act
            var getResponse = client.Execute<List<TestResult>>(getRequest);
            var getResponseDeserialized = JsonConvert.DeserializeObject<List<TestResult>>(getResponse.Content);

            //Assert
            Assert.AreEqual(true, getResponse.IsSuccessful);
            Assert.AreNotEqual(0, getResponseDeserialized.Count);
        }

        /// <summary>
        /// Will fail regulary because Get response mixes up IDs 
        /// and GetById returns fail half the times
        /// </summary>
        [Test]
        [TestCase(900, "AutoTestMan", "M", "2019-01-01", "BugLand", "Debug", "911")]
        [TestCase(900, "Debugger2", "F", "2019-01-01", "BugLand2", "Review2", "911a")]
        [TestCase(900, "Debugger2", "F", "2019-01-01", "BugLand2", "Review2", "")]
        [Order(20)]
        public void Test_20_Post_WhenSendTestresults_ShouldCreateNewResult(
            int id, string fullName, string gender, String birthDate, string city, string mainSkill, string phone)
        {
            TestResultPostRequestDto testResultPostRequestDto = default(TestResultPostRequestDto);
            //Arrange
            if (phone != string.Empty)
            {
                testResultPostRequestDto = new TestResultPostRequestDto
                {
                    id = id,
                    fullName = testPrefix + fullName,
                    gender = gender,
                    birthDate = birthDate,
                    city = city,
                    mainSkill = mainSkill,
                    phone = phone,
                };
            }
            else
            {
                testResultPostRequestDto = new TestResultPostRequestDto
                {
                    id = id,
                    fullName = testPrefix + fullName,
                    gender = gender,
                    birthDate = birthDate,
                    city = city,
                    mainSkill = mainSkill,
                };
            }


            postRequest.AddJsonBody(testResultPostRequestDto);

            //Act
            var postResponse = client.Execute<TestResult>(postRequest);
            var postResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(postResponse.Content);
            Assert.AreEqual(true, postResponse.IsSuccessful);
            CreatedEntitys.Add(postResponseDeserialized.id);

            var getResponse = client.Execute<TestResult>(getRequest);
            var getResponseDeserialized = JsonConvert.DeserializeObject<List<TestResult>>(getResponse.Content);

            var getByIdRequest = new RestRequest($"/superheroes/{postResponseDeserialized.id}", Method.GET);
            var getByIdResponse = client.Execute<TestResult>(getByIdRequest);
            Assert.AreEqual(true, getByIdResponse.IsSuccessful);
            var getByIdResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(getByIdResponse.Content);

            //Assert
            Assert.That(getResponseDeserialized.Any(x => x.id == postResponseDeserialized.id));
            Assert.That(getResponseDeserialized.Any(x => x.fullName == postResponseDeserialized.fullName));
            //Floating error on get by id like 1/2 times
            Assert.AreEqual(postResponseDeserialized.id, getByIdResponseDeserialized.id);
        }


        /// <summary>
        /// Will mostly fail begauce depends on GetByID wich fails 50% times
        /// And Put returns Error instead of proper response like 60% times.
        /// </summary>
        [Test]
        [Order(30)]
        public void Test_30_Put_WhenSendTestresults_ShouldRefreshWholeResult()
        {
            //Arrange
            var editedCity = "DebuggedLand";
            var NewEntry = new TestResultPostRequestDto
            {
                id = 900,
                fullName = testPrefix + "AutoTestMan",
                gender = "M",
                birthDate = "2019-01-01",
                city = "BugLand",
                mainSkill = "ForcedDebug",
                phone = "911",
            };
            postRequest.AddJsonBody(NewEntry);

            //Act
            var postRequestResponse = client.Execute<List<TestResult>>(postRequest);
            var postResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(postRequestResponse.Content);
            Assert.AreEqual(true, postRequestResponse.IsSuccessful);
            CreatedEntitys.Add(postResponseDeserialized.id);

            var putRequest = new RestRequest($"/superheroes/{postResponseDeserialized.id}", Method.PUT);
            putRequest.AddJsonBody(new TestResult
            {
                id = 900,
                fullName = testPrefix + "AutoTestMan",
                gender = "M",
                birthDate = "2019-01-01",
                city = editedCity,
                mainSkill = "ForcedDebug",
                phone = "911",
            });

            var putRequestResponse = client.Execute<TestResultPostRequestDto>(putRequest);
            Assert.AreEqual(true, putRequestResponse.IsSuccessful);

            var getByIdRequest = new RestRequest($"/superheroes/{postResponseDeserialized.id}", Method.GET);
            var getByIdResponse = client.Execute<TestResult>(getByIdRequest);
            Assert.AreEqual(true, getByIdResponse.IsSuccessful);
            var getByIdResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(getByIdResponse.Content);

            //Assert
            Assert.AreEqual(getByIdResponseDeserialized.city, editedCity);
        }

        /// <summary>
        /// Will fail almost allweys - becaude DELETE really deletes something
        /// on every third-fourth response. And GetById fails 50% requests
        /// </summary>
        [Test]
        [Order(40)]
        public void Test_40_Delete_WhenSendTestresults_ShouldDeleteExisting()
        {
            //Arrange
            var NewEntry = new TestResultPostRequestDto
            {
                id = 900,
                fullName = testPrefix + "AutoTestMan",
                gender = "M",
                birthDate = "2019-01-01",
                city = "BugLand",
                mainSkill = "ForcedDebug",
                phone = "911",
            };
            postRequest.AddJsonBody(NewEntry);

            //Act
            var postRequestResponse = client.Execute<TestResult>(postRequest);
            var postResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(postRequestResponse.Content);
            Assert.AreEqual(true, postRequestResponse.IsSuccessful);
            CreatedEntitys.Add(postResponseDeserialized.id);

            var deleteRequest = new RestRequest($"/superheroes/{postResponseDeserialized.id}", Method.DELETE);
            var deleteRequestResponse = client.Execute<TestResult>(deleteRequest);
            Assert.AreEqual(true, deleteRequestResponse.IsSuccessful);

            var getByIdRequest = new RestRequest($"/superheroes/{postResponseDeserialized.id}", Method.GET);
            var getByIdResponse = client.Execute<TestResult>(getByIdRequest);

            //Assert
            Assert.That(getByIdResponse.IsSuccessful, Is.False);
            Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        /// <summary>
        /// will fail because API has no check for string.Empty on required fields,
        /// except BirthDate, wich exepts any text instead of DateTime.
        /// </summary>
        [Test]
        [TestCase(900, "", "M", "2019-01-01", "BugLand", "Debug", "911")]
        [TestCase(-1, "Debugger", "U", "0001-01-01", "BugLand2", "Review2", "911a")]
        [TestCase(900, "Debugger", "U", "0001-01-01", "BugLand2", "Review2", "911a")]
        [TestCase(900, "Debugger2", "", "0001-01-01", "BugLand2", "Review2", "911a")]
        [TestCase(900, "Debugger3", "M", "", "BugLand2", "Review2", "911a")]
        [TestCase(900, "Debugger3", "M", "2019-01-01", "", "Review2", "911a")]
        [TestCase(900, "Debugger3", "M", "2019-01-01", "BugLand2", "", "911a")]
        [TestCase(900, "Debugger3", "M", "SomeText", "BugLand2", "Review2", "911a")]
        [Order(50)]
        public void Test_50_PostNegative_WhenSendBadTestresults_ShouldReturnBadRequest(
            int id, string fullName, string gender, String birthDate, string city, string mainSkill, string phone)
        {
            //Arrange
            var NewEntry = new TestResultPostRequestDto
            {
                id = id,
                fullName = testPrefix + fullName,
                gender = gender,
                birthDate = birthDate,
                city = city,
                mainSkill = mainSkill,
                phone = phone,
            };

            postRequest.AddJsonBody(NewEntry);

            //Act
            var postResponse = client.Execute<TestResult>(postRequest);
            Assert.AreEqual(false, postResponse.IsSuccessful);
            Assert.AreEqual(postResponse.StatusCode, HttpStatusCode.Forbidden);
        }

        [Test]
        [Order(60)]
        public void Test_60_PostNegative_WhenSendCorruptedTestresults_ShouldThrow()
        {
            //Arrange
            var NewBadEntry = new TestResultPostRequestDto
            {
                id = 900,
                gender = "M",
                birthDate = "2019-01-01",
                city = "BugLand",
                mainSkill = "ForcedDebug",
                phone = "911",
            };

            postRequest.AddJsonBody(NewBadEntry);

            //Act
            var postResponse = client.Execute<TestResult>(postRequest);
            Assert.AreEqual(false, postResponse.IsSuccessful);
            Assert.AreEqual(postResponse.StatusCode, HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Forced 10 retrys to overcome GetById problems
        /// </summary>
        [Test]
        [Order(70)]
        public void Test_70_AssumeRegressTesting()
        {
            var testResultPostRequestDto = new TestResultPostRequestDto
            {
                id = 900,
                fullName = testPrefix + "AutoTestMan",
                gender = "M",
                birthDate = "2019-01-01",
                city = "BugLand",
                mainSkill = "ForcedDebug",
                phone = "911",
            };

            postRequest.AddJsonBody(testResultPostRequestDto);

            //Act
            var postResponse = client.Execute<TestResult>(postRequest);
            var postResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(postResponse.Content);
            Assert.AreEqual(true, postResponse.IsSuccessful);
            CreatedEntitys.Add(postResponseDeserialized.id);

            var getResponse = client.Execute<TestResult>(getRequest);
            var getResponseDeserialized = JsonConvert.DeserializeObject<List<TestResult>>(getResponse.Content);

            IRestResponse<TestResult> getByIdResponse = default(IRestResponse<TestResult>);

            IRestResponse<TestResult> GetByIdResponse()
            {
                var getByIdRequest = new RestRequest($"/superheroes/{postResponseDeserialized.id}", Method.GET);
                var getByIdResponse = client.Execute<TestResult>(getByIdRequest);
                return getByIdResponse;
            }

            getByIdResponse = RetryHelper.Do(() => GetByIdResponse(), timeBetweenRetrys, retrysToFail);

            var getByIdResponseDeserialized = JsonConvert.DeserializeObject<TestResult>(getByIdResponse.Content);

            //Assert
            Assert.AreEqual(postResponseDeserialized.id, getByIdResponseDeserialized.id);
        }


        /// <summary>
        /// Clean up after tests, to remove trash from DB
        /// Uneffective due to DELETE not working properly
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (forceDbCleanup)
            {
                ForcedDbCleanupAndAssert();
            }
            else
            {
                DbCleanupAndAssert();
            }
        }

        private void ForcedDbCleanupAndAssert()
        {
            var getResponse = client.Execute<List<TestResult>>(getRequest);
            List<TestResult> getResponseDeserialized = JsonConvert.DeserializeObject<List<TestResult>>(getResponse.Content);

            foreach (var entity in getResponseDeserialized)
            {
                if(entity.fullName.Contains(testPrefix))
                {
                    for (int i = 0; i < forcedDbCleanupRetrys; i++)
                    {
                        var deleteRequest = new RestRequest($"/superheroes/{entity.id}", Method.DELETE);
                        var deleteRequestResponse = client.Execute<TestResult>(deleteRequest);
                        Assert.AreEqual(true, deleteRequestResponse.IsSuccessful);
                    }
                }
            }

            var FinalGetResponse = client.Execute<List<TestResult>>(getRequest);
            var FinalGetResponseDeserialized = JsonConvert.DeserializeObject<List<TestResult>>(getResponse.Content);

            Assert.AreEqual(false, getResponseDeserialized.Any(x => x.fullName.Contains(testPrefix)));
        }

        private void DbCleanupAndAssert()
        {
            foreach (var Entity in CreatedEntitys)
            {
                var deleteRequest = new RestRequest($"/superheroes/{Entity}", Method.DELETE);
                var deleteRequestResponse = client.Execute<TestResult>(deleteRequest);
                Assert.AreEqual(true, deleteRequestResponse.IsSuccessful);
            }

            var getResponse = client.Execute<List<TestResult>>(getRequest);
            var getResponseDeserialized = JsonConvert.DeserializeObject<List<TestResult>>(getResponse.Content);

            Assert.AreEqual(false, getResponseDeserialized.Any(x => x.fullName.Contains(testPrefix)));
        }
    }
}