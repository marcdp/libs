using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;
using System.Xml.Schema;

namespace DProjects.Utils.Tests {
    public class XmlUtilsTests {


        [Theory()]
        [InlineData("<a>holà<b></b></a>")]
        public void LoadXmlTest(string xml) {
            Assert.Equal(xml, XmlUtils.LoadXml(xml).OuterXml);
            Assert.Equal(xml, XmlUtils.LoadXml(new StringReader(xml)).OuterXml);
            Assert.Equal(xml, XmlUtils.LoadXml(new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)))).OuterXml);
        }

        [Fact()]
        public void LoadXmlFileTest() {
        }

        [Theory()]
        [InlineData("""
            <?xml version="1.0" encoding="UTF-8"?>
            <tns:PurchaseOrder xmlns:tns="http://tempuri.org/PurchaseOrderSchema.xsd" OrderDate="2024-02-29">
              <tns:ShipTo>
                <tns:name>John Doe</tns:name>
                <tns:street>123 Main Street</tns:street>
                <tns:city>Anytown</tns:city>
                <tns:state>CA</tns:state>
                <tns:zip>90210</tns:zip>
              </tns:ShipTo>
              <tns:ShipTo>
                <tns:name>Jane Smith</tns:name>
                <tns:street>456 Elm Street</tns:street>
                <tns:city>Anytown</tns:city>
                <tns:state>NY</tns:state>
                <tns:zip>10001</tns:zip>
              </tns:ShipTo>
              <tns:BillTo>
                <tns:name>Acme Corporation</tns:name>
                <tns:street>789 Business Park Dr</tns:street>
                <tns:city>Metropolis</tns:city>
                <tns:state>TX</tns:state>
                <tns:zip>77001</tns:zip>
              </tns:BillTo>
            </tns:PurchaseOrder>            
            """, """
            <xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                       xmlns:tns="http://tempuri.org/PurchaseOrderSchema.xsd"
                       targetNamespace="http://tempuri.org/PurchaseOrderSchema.xsd"
                       elementFormDefault="qualified">
             <xsd:element name="PurchaseOrder" type="tns:PurchaseOrderType"/>
             <xsd:complexType name="PurchaseOrderType">
              <xsd:sequence>
               <xsd:element name="ShipTo" type="tns:USAddress" maxOccurs="2"/>
               <xsd:element name="BillTo" type="tns:USAddress"/>
              </xsd:sequence>
              <xsd:attribute name="OrderDate" type="xsd:date"/>
             </xsd:complexType>

             <xsd:complexType name="USAddress">
              <xsd:sequence>
               <xsd:element name="name"   type="xsd:string"/>
               <xsd:element name="street" type="xsd:string"/>
               <xsd:element name="city"   type="xsd:string"/>
               <xsd:element name="state"  type="xsd:string"/>
               <xsd:element name="zip"    type="xsd:integer"/>
              </xsd:sequence>
              <xsd:attribute name="country" type="xsd:NMTOKEN" fixed="US"/>
             </xsd:complexType>
            </xsd:schema>
            """)]
        public void ValidateXmlDocumentAgainstSchemaTest(string xml, string xsd) {
            var xsdSchema = XmlSchema.Read(new StringReader(xsd), (sender, e) => { throw e.Exception; });
            XmlUtils.ValidateXmlDocumentAgainstSchema(XmlUtils.LoadXml(xml), xsdSchema, out string[] errors);
            Assert.Equal([], errors);
        }

        [Theory()]
        [InlineData("<a><b c=\"dd\">1</b><k>ò</k></a>", """
            <?xml version="1.0" encoding="utf-8"?>
            <a>
              <b c="dd">1</b>
              <k>ò</k>
            </a>
            """)]
        public void ToIndentedStringTest(string xml, string result) {
            var toIndented = XmlUtils.ToIndentedString(XmlUtils.LoadXml(xml));
            Assert.Equal(result, toIndented);
        }

        //[Fact()]
        //public void SetXmlChildNodeTest() {
        //    Assert.True(false, "This test needs an implementation");
        //}

        //[Fact()]
        //public void GetXmlChildNodeTest() {
        //    Assert.True(false, "This test needs an implementation");
        //}


        //[Fact()]
        //public void SetXmlAttributeTest() {
        //    Assert.True(false, "This test needs an implementation");
        //}


        //[Fact()]
        //public void GetXmlAttributeAsTest() {
        //    Assert.True(false, "This test needs an implementation");
        //}

        [Theory()]
        [InlineData("""
            <doc>
                <var name="var1" value="${var1}" />
                <var name="var2" value="${var2}" />
                <var name="var3" value="aaaa${var3}bbb" />
                <var name="var4" value="aaaa${var4}bbb${var5}ccc" />
            </doc>
        """, new string[] { "var1", "var2", "var3", "var4", "var5" })]
        public void ScanXmlAttributesForVariablesTest(string xml, string[]  variables) {
            var variablesScanned = new List<string>();
            XmlUtils.ScanXmlAttributesForVariables(XmlUtils.LoadXml(xml), (variable) => {
                variablesScanned.Add(variable);
                return "";
            });
            Assert.Equal(variables, variablesScanned);
        }
    }
}