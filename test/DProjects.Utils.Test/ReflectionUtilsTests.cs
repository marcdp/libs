using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class ReflectionUtilsTests {

        //aux classes
        public class MyClass {
            public int Field1;
            public int Property1 { get; set; }
            public int Sum(int inc) {
                return Field1 + Property1 + inc;
            }
        }


        //fields
        [Fact()]
        public void SetObjectFieldValueTest() {
            var field1 = 123;
            var myInstance = new MyClass();
            ReflectionUtils.SetObjectFieldValue(myInstance, "Field1", field1);
            Assert.Equal(field1, myInstance.Field1);
        }
        [Fact()]
        public void GetObjectFieldValueTest() {
            var myInstance = new MyClass() { Field1 = 1233 };
            Assert.Equal(myInstance.Field1, ReflectionUtils.GetObjectFieldValue(myInstance, "Field1"));
        }


        //props
        [Fact()]
        public void GetObjectPropertyValueTest() {
            var myInstance = new MyClass() { Property1 = 1233 };
            Assert.Equal(myInstance.Property1, ReflectionUtils.GetObjectPropertyValue(myInstance, "Property1"));
        }
        [Fact()]
        public void SetObjectPropertyValueTest() {
            var property1 = 123;
            var myInstance = new MyClass();
            ReflectionUtils.SetObjectPropertyValue(myInstance, "Property1", property1);
            Assert.Equal(property1, myInstance.Property1);
            ReflectionUtils.SetObjectPropertyValue(myInstance, "Property1", property1.ToString(), true);
            Assert.Equal(property1, myInstance.Property1);
        }


        //methods
        [Fact()]
        public void CallObjectMethodTest() {
            var myInstance = new MyClass() { Field1 = 10, Property1 = 20 };
            var inc = 30;
            Assert.Equal(myInstance.Sum(inc), ReflectionUtils.CallObjectMethod(myInstance, "Sum", [inc]));
            Assert.Equal(myInstance.Sum(inc), ReflectionUtils.CallObjectMethod(myInstance, "Sum", [inc.ToString()], true));
        }


    }
}