﻿using MTUComm.MemoryMap;
using System;
using System.Collections.Generic;
using Xunit;

using MTUComm;

// http://blog.benhall.me.uk/2008/01/introduction-to-xunit
// https://www.devexpress.com/Support/Center/Question/Details/T562649/test-runner-does-not-run-xunit-2-2-unit-tests-in-net-standard-2-0-project
namespace UnitTest.Tests
{
    public class Test_DynamicMemoryMap
    {
        #region Constants

        private const string ERROR  = "ERROR: ";
        private const string FOLDER = @"\Aclara_Test_Files\";

        private const string ERROR_VAL_STR      = ERROR + "String parameter is not a valid numeric value";
        private const string ERROR_VAL_INT      = ERROR + "Parameter is not a valid numeric integer";
        private const string ERROR_VAL_INT_NEG  = ERROR + "Parameter is not a valid negative numeric integer";
        private const string ERROR_VAL_UINT     = ERROR + "Parameter is not a valid numeric unsigned integer";
        private const string ERROR_VAL_ULONG    = ERROR + "Parameter is not a valid numeric unsigned long";
        private const string ERROR_VAL_BYTES    = ERROR + "Parameter value is too much larger for number of bytes";
        private const string ERROR_TYPE_INT     = ERROR + "Parameter value is outside limits for integer type";
        private const string ERROR_TYPE_UINT    = ERROR + "Parameter value is outside limits for unsigned integer type";
        private const string ERROR_TYPE_ULONG   = ERROR + "Parameter value is outside limits for unsigned long type";
        private const string ERROR_STRING_OUT   = ERROR + "String parameter has less or more characters than the limit";
        private const string ERROR_STRING_EMPTY = ERROR + "String parameter is empty";

        private const string ERROR_MMAP         = ERROR + "Dynamic mapping from XML has failed";
        private const string ERROR_RAW          = ERROR + "Work with raw or custom data have failed";
        private const string ERROR_REG_READONLY = ERROR + "Register readonly protection not works as expected";
        private const string ERROR_OVR_READONLY = ERROR + "Overloads readonly protection not works as expected";
        private const string ERROR_REG_CUS_GET  = ERROR + "Register custom get method not registered";
        private const string ERROR_REG_CUS_SET  = ERROR + "Register custom set method not registered";
        private const string ERROR_OVR_CUS_GET  = ERROR + "Overload custom get method not registered";
        private const string ERROR_REG_USE_GET  = ERROR + "Register custom get method not registered [ Use ]";
        private const string ERROR_REG_USE_SET  = ERROR + "Register custom set method not registered [ Use ]";
        private const string ERROR_OVR_USE_GET  = ERROR + "Overload custom get method not registered [ Use ]";
        private const string ERROR_REG_CUS_MIN  = ERROR + "Converting hours to minutes";
        private const string ERROR_BCD_ULONG_1  = ERROR + "Converting invoking BCD methods";
        private const string ERROR_BCD_ULONG_2  = ERROR + "Converting ULONG to BCD and vice versa";
        private const string ERROR_LIMIT_INT    = ERROR + "Setted value is larger than INT type limit";
        private const string ERROR_LIMIT_BYTES  = ERROR + "Setted value is larger than number of BYTES limit";

        #endregion

        #region Attributes

        private string exceptionError;

        #endregion

        #region Test methods

        private bool TestExpression ( Func<dynamic> func )
        {
            dynamic value = false;
            try
            {
                func.Invoke ();
            }
            catch ( Exception e )
            {
                this.exceptionError = e.Message;
                return false;
            }
            return true;
        }

        private string Error ( string constMessage = "" )
        {
            if ( ! string.IsNullOrEmpty ( constMessage ) )
                return constMessage + " => Message: " + this.exceptionError;
            return ERROR + this.exceptionError;
        }

        #endregion

        #region Tests

        [Fact]
        public void Test_Validations ()
        {
            // TEST: Validations
            // 1. IsNumeric
            string strNumGood = "1234";
            string strNumBad  = "12c34";
            string strNumNeg  = "-1234";
            int    numInt     = 1234;
            int    numIntNeg  = -5678;
            uint   numUInt    = 3000000000; // Int.max = 2147483647, UInt.max = 4294967295
            ulong  numULong   = 5000000000; // ULong.max = 18446744073709551615
            Assert.True ( Validations.IsNumeric<int>   ( strNumGood ), ERROR_VAL_STR     ); // true
            Assert.True ( ! Validations.IsNumeric<int> ( strNumBad  ), ERROR_VAL_STR     ); // false
            Assert.True ( Validations.IsNumeric<int>   ( strNumNeg  ), ERROR_VAL_STR     ); // true
            Assert.True ( Validations.IsNumeric<int>   ( numInt     ), ERROR_VAL_INT     ); // true
            Assert.True ( Validations.IsNumeric<int>   ( numIntNeg  ), ERROR_VAL_INT_NEG ); // true
            Assert.True ( Validations.IsNumeric<uint>  ( numUInt    ), ERROR_VAL_UINT    ); // true
            Assert.True ( Validations.IsNumeric<ulong> ( numULong   ), ERROR_VAL_ULONG   ); // true
            // 2. Limit by Bytes
            numInt   = 65535; // Last possible value using two bytes ( 2^16 = 65536 = {0-65535} )
            numULong = 65536;
            Assert.True ( Validations.NumericBytesLimit<int>     ( numInt,   2 ), ERROR_VAL_BYTES ); // true
            Assert.True ( ! Validations.NumericBytesLimit<ulong> ( numULong, 2 ), ERROR_VAL_BYTES ); // false
            // 3. Limit by Type
            Assert.True ( Validations.NumericTypeLimit<int>     ( int.MaxValue ), ERROR_TYPE_INT   ); // true
            Assert.True ( ! Validations.NumericTypeLimit<int>   ( "2147483648" ), ERROR_TYPE_INT   ); // false Int.max = 2147483647
            Assert.True ( Validations.NumericTypeLimit<int>     ( "-555"       ), ERROR_TYPE_INT   ); // true
            Assert.True ( Validations.NumericTypeLimit<int>     ( -5678        ), ERROR_TYPE_INT   ); // true
            Assert.True ( Validations.NumericTypeLimit<uint>    ( 5678         ), ERROR_TYPE_UINT  ); // true
            Assert.True ( Validations.NumericTypeLimit<ulong>   ( 2147483648   ), ERROR_TYPE_ULONG ); // true
            Assert.True ( ! Validations.NumericTypeLimit<ulong> ( -5678        ), ERROR_TYPE_ULONG ); // false
            // 4. Strings validations
            string str1 = "texto de prueba";
            string str2 = string.Empty;
            Assert.True ( Validations.TextLength   ( str1, 20,   5 ), ERROR_STRING_OUT   ); // true
            Assert.True ( ! Validations.TextLength ( str1, 10,   5 ), ERROR_STRING_OUT   ); // false More chars
            Assert.True ( ! Validations.TextLength ( str1, 100, 20 ), ERROR_STRING_OUT   ); // false Less chars
            Assert.True ( ! Validations.TextLength ( str2, 20,   5 ), ERROR_STRING_EMPTY ); // false
        }

        [Theory]
        [InlineData("family_31xx32xx_test1")]
        //[InlineData("family_31xx32xx_test2")]
        //[InlineData("family_31xx32xx_test3")]
        public void Test_GenerateMemoryMapFromXml ( string xmlName )
        {
            Func<Func<dynamic>,bool> test = this.TestExpression;

            string path = Environment.GetFolderPath ( Environment.SpecialFolder.Desktop );

            byte[] memory = new byte[400];

            // Dynamic memory map generation
            dynamic map = null;
            Assert.True ( test(() => { return map = new MemoryMap ( memory, xmlName, path + FOLDER ); }), Error ( ERROR_MMAP ) );

            // If memory map can't be created, test finishes
            if ( map == null )
                return;

            // TEST: Value raw y processed
            MemoryRegister<ulong> p1mid = map.GetProperty ( "P1MeterId" );
            p1mid.Value = "357"; // --BCD--> 001101010111 = 855
            ulong valueBcd2Ulong = p1mid.Value;    // 001101010111 --DECIMAL--> 357 ( value in decimal )
            ulong rawValue       = p1mid.ValueRaw; // 855 ( value in BCD )
            Assert.True ( valueBcd2Ulong == 357 && rawValue == 855, ERROR_RAW );

            // TEST: Readonly
            Assert.False ( ! test ( () => { return map.MtuType == 123; } ), ERROR_REG_READONLY ); // Register
            Assert.False ( ! test ( () => { return map.ReadInterval == "24 Hours"; } ), ERROR_OVR_READONLY ); // Overload

            // TEST: Custom methods
            MemoryRegister<ulong>  p1MeterId    = map.GetProperty ( "P1MeterId"    );
            MemoryOverload<string> readInterval = map.GetProperty ( "ReadInterval" );
            // 1. Methods references created
            Assert.True ( p1MeterId.funcGetCustom != null, ERROR_REG_CUS_GET );
            Assert.True ( p1MeterId.funcSetCustom != null, ERROR_REG_CUS_SET );
            Assert.True ( readInterval.funcGet    != null, ERROR_OVR_CUS_GET );
            // 2. Use methods
            Assert.True ( test(() => { return map.P1MeterId = 22; }), ERROR_REG_USE_SET ); // Register use set
            Assert.True ( test(() => { return map.P1MeterId;      }), ERROR_REG_USE_GET ); // Register use get
            Assert.True ( test(() => { return map.ReadInterval;   }), ERROR_OVR_USE_GET ); // Overload use get

            // TEST: Custom method to convert hours to minutes
            map.ReadIntervalMinutes = "24 Hours"; // On memory writes in minutes: 24 * 60 = 1440
            Assert.True ( map.ReadIntervalMinutes == 24, ERROR_REG_CUS_MIN );

            // TEST: Custom BCD methods ( get = bcd to ulong, set = ulong to bcd )
            map.P1MeterId = 1234; // En memoria escribe 0x34 y 0x12
            Assert.True ( map.P1MeterId == 1234, ERROR_BCD_ULONG_1 );
            Assert.True ( memory[ p1MeterId.address     ] == 0x34, ERROR_BCD_ULONG_2 );
            Assert.True ( memory[ p1MeterId.address + 1 ] == 0x12, ERROR_BCD_ULONG_2 );

            // TEST: Limit INT ( 2^16 = 65536 )
            //map.P1MeterType = 65538; // Overflow and sets 2 ( 65538 - 65536 )
            //Assert.True ( map.P1MeterType <= 65536, ERROR_LIMIT_INT );
            //map.P1MeterType = 65535; // Not overflow and set 
            //Assert.True ( map.P1MeterType == 65535, ERROR_LIMIT_INT);
            // 1. Value is outside assigned bytes limit
            Assert.True ( ! test(() => { return map.P1MeterType = 65536; }), ERROR_LIMIT_BYTES ); // int 2 bytes ( 2^16 = 65536 )
            // 2. Value is outside type limit ( int max. 2147483647 )
            Assert.True ( ! test(() => { return map.P1MeterId   = 281474976710656; }), ERROR_LIMIT_INT ); // int 6 bytes ( 2^48 = 281,474.976,710.656 )

            return;

            // TEST: Recover only modified registers
            map.P1Reading       = 2;     // ulong
            map.P2Reading       = "2";   // ulong
            map.EncryptionKey   = "key"; // string
            map.Encrypted       = true;  // bool
            map.PCBSupplierCode = 2;     // int
            List<dynamic> mods = map.GetModifiedRegisters ().GetAllElements ();
            Assert.True ( mods.Count == 5, "FAIL!" );

            // TEST: Diferentes opciones campo custom ( metodo y operacion matematica )
            //Console.WriteLine ( "Test operation register: " + base.registers.BatteryVoltage );
            //Console.WriteLine ( "Test custom format: " + base.registers.DailyRead );

            // TEST: Separacion entre Value.get y funGetCustom
            //dynamic mInt = this.GetProperty_Int ( "DailyRead" );
            //Console.WriteLine ( base.registers.DailyRead + " == " + mInt.Value );
            //mInt.Value = 123;
            //Console.WriteLine ( base.registers.DailyRead + " == " + mInt.Value );

            // TEST: Recuperar registros modificados
            //this.SetRegisterModified ( "MtuType"   );
            //this.SetRegisterModified ( "Shipbit"   );
            //this.SetRegisterModified ( "DailyRead" );
            //MemoryRegisterDictionary regs = this.GetModifiedRegisters ();

            // TEST: Recuperar objetos registro
            //dynamic             reg1 = this.GetProperty      ( "MtuType" );
            //MemoryRegister<int> reg2 = this.GetProperty<int> ( "MtuType" );
            //MemoryRegister<int> reg3 = this.GetProperty_Int  ( "MtuType" );
            //Console.WriteLine ( "Registro MtuType: " +
            //    reg1.Value + " " + reg2.Value + " " + reg3.Value );

            // TEST: Trabajar con overloads
            //Console.WriteLine ( "Test metodo overload: "       + base.registers.Overload_Method );
            //Console.WriteLine ( "Test metodo reuse overload: " + base.registers.Overload_Method_Reuse );
            //Console.WriteLine ( "Test metodo array overload: " + base.registers.Overload_Method_Array );
            //Console.WriteLine ( "Test operation overload: "    + base.registers.Overload_Operation );
        }

        #endregion
    }
}