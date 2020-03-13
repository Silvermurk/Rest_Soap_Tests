using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace RestApi
{
    /// <summary>
    /// Somewhy SOAP response is not a XML serealizable format. 
    /// So had to make addon to get direct SOAP response and
    /// parse it with RegEx.
    /// Skipped on negative tests, not to overexhaust project with XMLs
    /// </summary>
    [Parallelizable(ParallelScope.Children)]
    class SOAPTests
    {
        private string testPrefix = "Autotest_";
        private string url = @"https://soap.qa-test.csssr.com/ws/soap.wsdl";
        private string method = @"";
        private string addCompanyRequest;
        private string addEmployeeRequest;
        private string addEmployeeToCompanyRequest;
        private string addMultipleEmployeesToCompanyRequest;
        private string getCompanyRequest;
        private string updateEmployeeRequest;

        [SetUp]
        public void SetUp()
        {
            addCompanyRequest = File.ReadAllText(@"SoapXmls\AddCompanyRequest.xml");
            addEmployeeRequest = File.ReadAllText(@"SoapXmls\addEmployeeRequest.xml");
            addEmployeeToCompanyRequest = File.ReadAllText(@"SoapXmls\addEmployeeToCompanyRequest.xml");
            addMultipleEmployeesToCompanyRequest = File.ReadAllText(@"SoapXmls\addMultipleEmployeesToCompanyRequest.xml");
            getCompanyRequest = File.ReadAllText(@"SoapXmls\GetCompanyRequest.xml");
            updateEmployeeRequest = File.ReadAllText(@"SoapXmls\UpdateEmployeeRequest.xml");
        }


        [Test]
        public void Test_10_AddCompany_ShouldBeOk()
        {
            //Arrange
            string companyName = testPrefix + "AutotestCompany";

            //Replace placeholder name with prefixed name
            addCompanyRequest = Regex.Replace(addCompanyRequest, "TestCompanyName", companyName);

            //Act
            var response = GetResponseSoap(url, method, addCompanyRequest);
            var regexNamePattern = @"(?<=<ns2:Name>)(.*?)(?=<\/ns2:Name>)";
            Regex regex = new Regex(regexNamePattern, RegexOptions.Singleline);
            var responseCompanyName = regex.Matches(response);

            //Assert
            Assert.AreEqual(responseCompanyName.Single().Value, companyName);
        }

        [Test]
        public void Test_20_AddEmployee_ShouldBeOk()
        {
            //Arrange
            string lastName = testPrefix + "AutotestLastName";
            //Replace LastName placeholder with prefixed name
            addEmployeeRequest = Regex.Replace(addEmployeeRequest, "AutotestLastName", lastName);

            //Act
            var response = GetResponseSoap(url, method, addEmployeeRequest);
            var regexNamePattern = @"(?<=<ns2:LastName>)(.*?)(?=<\/ns2:LastName>)";
            Regex regex = new Regex(regexNamePattern, RegexOptions.Singleline);
            var responseEmployeeName = regex.Matches(response);

            //Assert
            Assert.AreEqual(responseEmployeeName.Single().Value, lastName);
        }

        [Test]
        public void Test_30_AddEmployeeToCompany_ShouldBeOk()
        {
            //Arrange
            string lastName = testPrefix + "AutotestLastName";
            string companyName = testPrefix + "AutotestCompany";
            //Replace company and employees last name with prefixed ones
            addCompanyRequest = Regex.Replace(addCompanyRequest, "TestCompanyName", companyName);
            addEmployeeRequest = Regex.Replace(addEmployeeRequest, "AutotestLastName", lastName);

            //Act
            var addCompanyResponse = GetResponseSoap(url, method, addCompanyRequest);
            var addEmployeeResponse = GetResponseSoap(url, method, addEmployeeRequest);
            var regexIdPattern = @"(?<=<ns2:Id>)(.*?)(?=<\/ns2:Id>)";
            Regex regex = new Regex(regexIdPattern, RegexOptions.Singleline);
            //Get company and employee`s IDs
            var responseCompanyId = regex.Matches(addCompanyResponse);
            var responseEmployeeId = regex.Matches(addEmployeeResponse);
            //Replace placeholder IDs inside SOAP AddEmployeeToCompany with real ones
            addEmployeeToCompanyRequest = Regex.Replace(addEmployeeToCompanyRequest, "TestCompanyId", responseCompanyId.Single().Value);
            addEmployeeToCompanyRequest = Regex.Replace(addEmployeeToCompanyRequest, "TestEmployeeId", responseEmployeeId.Single().Value);
            var addEmployeeToCompanyResponse = GetResponseSoap(url, method, addEmployeeToCompanyRequest);

            //Assert
            Assert.IsTrue(addEmployeeToCompanyResponse.Contains(responseCompanyId.Single().Value));
            Assert.IsTrue(addEmployeeToCompanyResponse.Contains(responseEmployeeId.Single().Value));
        }

        [Test]
        public void Test_40_GetCompany_ShouldBeOk()
        {
            //Arrange
            string companyName = testPrefix + "AutotestCompany";
            //Replace company placeholder name with prefixed one
            addCompanyRequest = Regex.Replace(addCompanyRequest, "TestCompanyName", companyName);

            //Act
            var AddCompanyResponse = GetResponseSoap(url, method, addCompanyRequest);
            //Get company ID from response
            var regexIdPattern = @"(?<=<ns2:Id>)(.*?)(?=<\/ns2:Id>)";
            Regex regex = new Regex(regexIdPattern, RegexOptions.Singleline);
            var responseAddCompanyId = regex.Matches(AddCompanyResponse);
            //Replace company ID placeholder in envelope with real one
            getCompanyRequest = Regex.Replace(getCompanyRequest, "TestCompanyId", responseAddCompanyId.Single().Value);
            var GetCompanyResponse = GetResponseSoap(url, method, getCompanyRequest);
            var responseGetCompanyId = regex.Matches(GetCompanyResponse);

            //Assert
            Assert.AreEqual(responseAddCompanyId.Single().Value, responseGetCompanyId.Single().Value);
        }

        /// <summary>
        /// Will fail last assert bevause API allweys returns one employee per company
        /// </summary>
        [Test]
        public void Test_50_AddMultipleEmployeesToCompany_ShouldBeOk()
        {
            //Arrange
            string lastName = testPrefix + "AutotestLastName";
            string companyName = testPrefix + "AutotestCompany";
            //Replace company ad Employees name placeholders with real ones
            addCompanyRequest = Regex.Replace(addCompanyRequest, "TestCompanyName", companyName);
            addEmployeeRequest = Regex.Replace(addEmployeeRequest, "AutotestLastName", lastName);

            //Act
            var addCompanyResponse = GetResponseSoap(url, method, addCompanyRequest);
            var addEmployeeResponse = GetResponseSoap(url, method, addEmployeeRequest);
            var AddSecondEmployeeResponse = GetResponseSoap(url, method, addEmployeeRequest);
            //Get company and employee IDs from responses
            var regexIdPattern = @"(?<=<ns2:Id>)(.*?)(?=<\/ns2:Id>)";
            Regex regex = new Regex(regexIdPattern, RegexOptions.Singleline);
            var responseCompanyId = regex.Matches(addCompanyResponse);
            var responseEmployeeId = regex.Matches(addEmployeeResponse);
            var responseSecondEmployeeId = regex.Matches(AddSecondEmployeeResponse);

            //Replace ID placeholders in SOAP AddMultipleEmployeesToCompany with real ones
            addMultipleEmployeesToCompanyRequest = Regex.Replace(
                addMultipleEmployeesToCompanyRequest,
                "TestCompanyId",
                responseCompanyId.Single().Value);

            addMultipleEmployeesToCompanyRequest = Regex.Replace(
                addMultipleEmployeesToCompanyRequest,
                "TestEmployeeId",
                responseEmployeeId.Single().Value);

            addMultipleEmployeesToCompanyRequest = Regex.Replace(
                addMultipleEmployeesToCompanyRequest,
                "TestSecondEmployeeId",
                responseSecondEmployeeId.Single().Value);

            var addMultipleEmployeesResponse = GetResponseSoap(url, method, addMultipleEmployeesToCompanyRequest);

            //Assert
            Assert.IsTrue(addMultipleEmployeesResponse.Contains(responseCompanyId.Single().Value));
            Assert.IsTrue(addMultipleEmployeesResponse.Contains(responseEmployeeId.Single().Value));
            Assert.IsTrue(addMultipleEmployeesResponse.Contains(responseSecondEmployeeId.Single().Value));
        }

        /// <summary>
        /// Will fail last assert because of UpdatedAt never changes
        /// </summary>
        [Test]
        public void Test_60_UpdateEmployee_ShouldBeOk()
        {
            //Arrange
            string lastName = testPrefix + "LastName";
            string newlastName = testPrefix + "NewLastName";
            string firstName = testPrefix + "FirstName";
            string newFirstName = testPrefix + "NewFirstName";
            //Replace LaseName placeholders in Add And Update request envelopes
            addEmployeeRequest = Regex.Replace(addEmployeeRequest, "AutotestFirstName", firstName);
            addEmployeeRequest = Regex.Replace(addEmployeeRequest, "AutotestLastName", lastName);
            updateEmployeeRequest = Regex.Replace(updateEmployeeRequest, "AutotestFirstName", newFirstName);
            updateEmployeeRequest = Regex.Replace(updateEmployeeRequest, "AutotestLastName", newlastName);

            //Act
            var addEmployeeResponse = GetResponseSoap(url, method, addEmployeeRequest);
            //Get ID from add Employee response
            var regexIdPattern = @"(?<=<ns2:Id>)(.*?)(?=<\/ns2:Id>)";
            Regex regex = new Regex(regexIdPattern, RegexOptions.Singleline);
            var AddEmployeeResponseId = regex.Matches(addEmployeeResponse);
            //And replace placeholder in Update enveloper with one we got from Add
            updateEmployeeRequest = Regex.Replace(updateEmployeeRequest, "TestEmployeeId", AddEmployeeResponseId.Single().Value);
            //Run Update method and get Employee ID from there
            var updateEmployeeResponse = GetResponseSoap(url, method, updateEmployeeRequest);
            var updateEmployeeResponseId = regex.Matches(updateEmployeeResponse);
            //Get last names from Add and Update responses
            var regexNamePattern = @"(?<=<ns2:LastName>)(.*?)(?=<\/ns2:LastName>)";
            regex = new Regex(regexNamePattern, RegexOptions.Singleline);
            //var addEmployeeResponseName = regex.Matches(addEmployeeResponse);
            var updateEmployeeResponseName = regex.Matches(updateEmployeeResponse);
            //Get UpdatedAt timestamp
            var regexDatePattern = @"(?<=<ns2:UpdatedAt>)(.*?)(?=<\/ns2:UpdatedAt>)";
            regex = new Regex(regexDatePattern, RegexOptions.Singleline);
            var AddEmployeeResponseUpdatedAt = regex.Matches(addEmployeeResponse);
            var UpdateEmployeeResponseUpdatedAt = regex.Matches(updateEmployeeResponse);

            //Assert
            Assert.AreEqual(AddEmployeeResponseId.Single().Value, updateEmployeeResponseId.Single().Value);
            Assert.AreEqual(updateEmployeeResponseName.Single().Value, newlastName);
            //Convert string to DateTime and assert that Employee was updated later than created
            Assert.IsTrue(Convert.ToDateTime(AddEmployeeResponseUpdatedAt.Single().Value) < Convert.ToDateTime(UpdateEmployeeResponseUpdatedAt.Single().Value));
        }


        private string GetResponseSoap(string url, string method, string soapEnvelope)
        {
            url = url.Trim('/').Trim('\\');
            WebRequest _request = HttpWebRequest.Create(url);
            _request.Method = "POST";
            _request.ContentType = "text/xml; charset=utf-8";
            _request.ContentLength = soapEnvelope.Length;
            _request.Headers.Add("SOAPAction", url + @"/" + method);

            StreamWriter _streamWriter = new StreamWriter(_request.GetRequestStream());
            _streamWriter.Write(soapEnvelope);
            _streamWriter.Close();

            WebResponse _response = _request.GetResponse();
            StreamReader _streamReader = new StreamReader(_response.GetResponseStream());
            string _result = _streamReader.ReadToEnd();
            return _result;
        }
    }
}
