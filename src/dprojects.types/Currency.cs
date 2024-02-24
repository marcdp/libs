namespace DProjects.Types {

    //enum ISO_4217
    public enum Currency {
        AED, //United Arab Emirates Dirham
        AFN, //Afghanistan Afghani
        ALL, //Albania Lek
        AMD, //Armenia Dram
        ANG, //Netherlands Antilles Guilder
        AOA, //Angola Kwanza
        ARS, //Argentina Peso
        AUD, //Australia Dollar
        AWG, //Aruba Guilder
        AZN, //Azerbaijan Manat
        BAM, //Bosnia and Herzegovina Convertible Marka
        BBD, //Barbados Dollar
        BDT, //Bangladesh Taka
        BGN, //Bulgaria Lev
        BHD, //Bahrain Dinar
        BIF, //Burundi Franc
        BMD, //Bermuda Dollar
        BND, //Brunei Darussalam Dollar
        BOB, //Bolivia Bolíviano
        BRL, //Brazil Real
        BSD, //Bahamas Dollar
        BTN, //Bhutan Ngultrum
        BWP, //Botswana Pula
        BYN, //Belarus Ruble
        BZD, //Belize Dollar
        CAD, //Canada Dollar
        CDF, //Congo/Kinshasa Franc
        CHF, //Switzerland Franc
        CLP, //Chile Peso
        CNY, //China Yuan Renminbi
        COP, //Colombia Peso
        CRC, //Costa Rica Colon
        CUC, //Cuba Convertible Peso
        CUP, //Cuba Peso
        CVE, //Cape Verde Escudo
        CZK, //Czech Republic Koruna
        DJF, //Djibouti Franc
        DKK, //Denmark Krone
        DOP, //Dominican Republic Peso
        DZD, //Algeria Dinar
        EGP, //Egypt Pound
        EMF, //
        ERN, //Eritrea Nakfa
        ETB, //Ethiopia Birr
        EUR, //Euro Member Countries
        FJD, //Fiji Dollar
        FKP, //Falkland Islands (Malvinas) Pound
        GBP, //United Kingdom Pound
        GEL, //Georgia Lari
        GGP, //Guernsey Pound
        GHS, //Ghana Cedi
        GIP, //Gibraltar Pound
        GMD, //Gambia Dalasi
        GNF, //Guinea Franc
        GTQ, //Guatemala Quetzal
        GYD, //Guyana Dollar
        HKD, //Hong Kong Dollar
        HNL, //Honduras Lempira
        HRK, //Croatia Kuna
        HTG, //Haiti Gourde
        HUF, //Hungary Forint
        IDR, //Indonesia Rupiah
        ILS, //Israel Shekel
        IMP, //Isle of Man Pound
        INR, //India Rupee
        IQD, //Iraq Dinar
        IRR, //Iran Rial
        ISK, //Iceland Krona
        JEP, //Jersey Pound
        JMD, //Jamaica Dollar
        JOD, //Jordan Dinar
        JPY, //Japan Yen
        KES, //Kenya Shilling
        KGS, //Kyrgyzstan Som
        KHR, //Cambodia Riel
        KMF, //Comorian Franc
        KPW, //Korea (North) Won
        KRW, //Korea (South) Won
        KWD, //Kuwait Dinar
        KYD, //Cayman Islands Dollar
        KZT, //Kazakhstan Tenge
        LAK, //Laos Kip
        LBP, //Lebanon Pound
        LKR, //Sri Lanka Rupee
        LRD, //Liberia Dollar
        LSL, //Lesotho Loti
        LYD, //Libya Dinar
        MAD, //Morocco Dirham
        MDL, //Moldova Leu
        MGA, //Madagascar Ariary
        MKD, //Macedonia Denar
        MMK, //Myanmar (Burma) Kyat
        MNT, //Mongolia Tughrik
        MOP, //Macau Pataca
        MRU, //Mauritania Ouguiya
        MUR, //Mauritius Rupee
        MVR, //Maldives (Maldive Islands) Rufiyaa
        MWK, //Malawi Kwacha
        MXN, //Mexico Peso
        MYR, //Malaysia Ringgit
        MZN, //Mozambique Metical
        NAD, //Namibia Dollar
        NGN, //Nigeria Naira
        NIO, //Nicaragua Cordoba
        NOK, //Norway Krone
        NPR, //Nepal Rupee
        NZD, //New Zealand Dollar
        OMR, //Oman Rial
        PAB, //Panama Balboa
        PEN, //Peru Sol
        PGK, //Papua New Guinea Kina
        PHP, //Philippines Piso
        PKR, //Pakistan Rupee
        PLN, //Poland Zloty
        PYG, //Paraguay Guarani
        QAR, //Qatar Riyal
        RON, //Romania Leu
        RSD, //Serbia Dinar
        RUB, //Russia Ruble
        RWF, //Rwanda Franc
        SAR, //Saudi Arabia Riyal
        SBD, //Solomon Islands Dollar
        SCR, //Seychelles Rupee
        SDG, //Sudan Pound
        SEK, //Sweden Krona
        SGD, //Singapore Dollar
        SHP, //Saint Helena Pound
        SLL, //Sierra Leone Leone
        SOS, //Somalia Shilling
        SPL, //Seborga Luigino
        SRD, //Suriname Dollar
        STN, //São Tomé and Príncipe Dobra
        SVC, //El Salvador Colon
        SYP, //Syria Pound
        SZL, //Swaziland Lilangeni
        THB, //Thailand Baht
        TJS, //Tajikistan Somoni
        TMT, //Turkmenistan Manat
        TND, //Tunisia Dinar
        TOP, //Tonga Pa'anga
        TRY, //Turkey Lira
        TTD, //Trinidad and Tobago Dollar
        TVD, //Tuvalu Dollar
        TWD, //Taiwan New Dollar
        TZS, //Tanzania Shilling
        UAH, //Ukraine Hryvnia
        UGX, //Uganda Shilling
        USD, //United States Dollar
        UYU, //Uruguay Peso
        UZS, //Uzbekistan Som
        VEF, //Venezuela Bolívar
        VND, //Viet Nam Dong
        VUV, //Vanuatu Vatu
        WST, //Samoa Tala
        XAF, //Communauté Financière Africaine (BEAC) CFA Franc BEAC
        XCD, //East Caribbean Dollar
        XDR, //International Monetary Fund (IMF) Special Drawing Rights
        XOF, //Communauté Financière Africaine (BCEAO) Franc
        XPF, //Comptoirs Français du Pacifique (CFP) Franc
        YER, //Yemen Rial
        ZAR, //South Africa Rand
        ZMW, //Zambia Kwacha
        ZWD, //Zimbabwe Dollar

        //historical, not active
        TRL, //Turkish lira
        CNH, //Chinese yuan (when traded offshore) used in 	Hong Kong
    }

    public static class CurrencyModule {

        //methods
        public static string Description(this Currency c) {
            if (c == Currency.AED) return "United Arab Emirates Dirham";
            if (c == Currency.AFN) return "Afghanistan Afghani";
            if (c == Currency.ALL) return "Albania Lek";
            if (c == Currency.AMD) return "Armenia Dram";
            if (c == Currency.ANG) return "Netherlands Antilles Guilder";
            if (c == Currency.AOA) return "Angola Kwanza";
            if (c == Currency.ARS) return "Argentina Peso";
            if (c == Currency.AUD) return "Australia Dollar";
            if (c == Currency.AWG) return "Aruba Guilder";
            if (c == Currency.AZN) return "Azerbaijan Manat";
            if (c == Currency.BAM) return "Bosnia and Herzegovina Convertible Marka";
            if (c == Currency.BBD) return "Barbados Dollar";
            if (c == Currency.BDT) return "Bangladesh Taka";
            if (c == Currency.BGN) return "Bulgaria Lev";
            if (c == Currency.BHD) return "Bahrain Dinar";
            if (c == Currency.BIF) return "Burundi Franc";
            if (c == Currency.BMD) return "Bermuda Dollar";
            if (c == Currency.BND) return "Brunei Darussalam Dollar";
            if (c == Currency.BOB) return "Bolivia Bolíviano";
            if (c == Currency.BRL) return "Brazil Real";
            if (c == Currency.BSD) return "Bahamas Dollar";
            if (c == Currency.BTN) return "Bhutan Ngultrum";
            if (c == Currency.BWP) return "Botswana Pula";
            if (c == Currency.BYN) return "Belarus Ruble";
            if (c == Currency.BZD) return "Belize Dollar";
            if (c == Currency.CAD) return "Canada Dollar";
            if (c == Currency.CDF) return "Congo/Kinshasa Franc";
            if (c == Currency.CHF) return "Switzerland Franc";
            if (c == Currency.CLP) return "Chile Peso";
            if (c == Currency.CNY) return "China Yuan Renminbi";
            if (c == Currency.COP) return "Colombia Peso";
            if (c == Currency.CRC) return "Costa Rica Colon";
            if (c == Currency.CUC) return "Cuba Convertible Peso";
            if (c == Currency.CUP) return "Cuba Peso";
            if (c == Currency.CVE) return "Cape Verde Escudo";
            if (c == Currency.CZK) return "Czech Republic Koruna";
            if (c == Currency.DJF) return "Djibouti Franc";
            if (c == Currency.DKK) return "Denmark Krone";
            if (c == Currency.DOP) return "Dominican Republic Peso";
            if (c == Currency.DZD) return "Algeria Dinar";
            if (c == Currency.EGP) return "Egypt Pound";
            if (c == Currency.ERN) return "Eritrea Nakfa";
            if (c == Currency.ETB) return "Ethiopia Birr";
            if (c == Currency.EUR) return "Euro Member Countries";
            if (c == Currency.FJD) return "Fiji Dollar";
            if (c == Currency.FKP) return "Falkland Islands (Malvinas) Pound";
            if (c == Currency.GBP) return "United Kingdom Pound";
            if (c == Currency.GEL) return "Georgia Lari";
            if (c == Currency.GGP) return "Guernsey Pound";
            if (c == Currency.GHS) return "Ghana Cedi";
            if (c == Currency.GIP) return "Gibraltar Pound";
            if (c == Currency.GMD) return "Gambia Dalasi";
            if (c == Currency.GNF) return "Guinea Franc";
            if (c == Currency.GTQ) return "Guatemala Quetzal";
            if (c == Currency.GYD) return "Guyana Dollar";
            if (c == Currency.HKD) return "Hong Kong Dollar";
            if (c == Currency.HNL) return "Honduras Lempira";
            if (c == Currency.HRK) return "Croatia Kuna";
            if (c == Currency.HTG) return "Haiti Gourde";
            if (c == Currency.HUF) return "Hungary Forint";
            if (c == Currency.IDR) return "Indonesia Rupiah";
            if (c == Currency.ILS) return "Israel Shekel";
            if (c == Currency.IMP) return "Isle of Man Pound";
            if (c == Currency.INR) return "India Rupee";
            if (c == Currency.IQD) return "Iraq Dinar";
            if (c == Currency.IRR) return "Iran Rial";
            if (c == Currency.ISK) return "Iceland Krona";
            if (c == Currency.JEP) return "Jersey Pound";
            if (c == Currency.JMD) return "Jamaica Dollar";
            if (c == Currency.JOD) return "Jordan Dinar";
            if (c == Currency.JPY) return "Japan Yen";
            if (c == Currency.KES) return "Kenya Shilling";
            if (c == Currency.KGS) return "Kyrgyzstan Som";
            if (c == Currency.KHR) return "Cambodia Riel";
            if (c == Currency.KMF) return "Comorian Franc";
            if (c == Currency.KPW) return "Korea (North) Won";
            if (c == Currency.KRW) return "Korea (South) Won";
            if (c == Currency.KWD) return "Kuwait Dinar";
            if (c == Currency.KYD) return "Cayman Islands Dollar";
            if (c == Currency.KZT) return "Kazakhstan Tenge";
            if (c == Currency.LAK) return "Laos Kip";
            if (c == Currency.LBP) return "Lebanon Pound";
            if (c == Currency.LKR) return "Sri Lanka Rupee";
            if (c == Currency.LRD) return "Liberia Dollar";
            if (c == Currency.LSL) return "Lesotho Loti";
            if (c == Currency.LYD) return "Libya Dinar";
            if (c == Currency.MAD) return "Morocco Dirham";
            if (c == Currency.MDL) return "Moldova Leu";
            if (c == Currency.MGA) return "Madagascar Ariary";
            if (c == Currency.MKD) return "Macedonia Denar";
            if (c == Currency.MMK) return "Myanmar (Burma) Kyat";
            if (c == Currency.MNT) return "Mongolia Tughrik";
            if (c == Currency.MOP) return "Macau Pataca";
            if (c == Currency.MRU) return "Mauritania Ouguiya";
            if (c == Currency.MUR) return "Mauritius Rupee";
            if (c == Currency.MVR) return "Maldives (Maldive Islands) Rufiyaa";
            if (c == Currency.MWK) return "Malawi Kwacha";
            if (c == Currency.MXN) return "Mexico Peso";
            if (c == Currency.MYR) return "Malaysia Ringgit";
            if (c == Currency.MZN) return "Mozambique Metical";
            if (c == Currency.NAD) return "Namibia Dollar";
            if (c == Currency.NGN) return "Nigeria Naira";
            if (c == Currency.NIO) return "Nicaragua Cordoba";
            if (c == Currency.NOK) return "Norway Krone";
            if (c == Currency.NPR) return "Nepal Rupee";
            if (c == Currency.NZD) return "New Zealand Dollar";
            if (c == Currency.OMR) return "Oman Rial";
            if (c == Currency.PAB) return "Panama Balboa";
            if (c == Currency.PEN) return "Peru Sol";
            if (c == Currency.PGK) return "Papua New Guinea Kina";
            if (c == Currency.PHP) return "Philippines Piso";
            if (c == Currency.PKR) return "Pakistan Rupee";
            if (c == Currency.PLN) return "Poland Zloty";
            if (c == Currency.PYG) return "Paraguay Guarani";
            if (c == Currency.QAR) return "Qatar Riyal";
            if (c == Currency.RON) return "Romania Leu";
            if (c == Currency.RSD) return "Serbia Dinar";
            if (c == Currency.RUB) return "Russia Ruble";
            if (c == Currency.RWF) return "Rwanda Franc";
            if (c == Currency.SAR) return "Saudi Arabia Riyal";
            if (c == Currency.SBD) return "Solomon Islands Dollar";
            if (c == Currency.SCR) return "Seychelles Rupee";
            if (c == Currency.SDG) return "Sudan Pound";
            if (c == Currency.SEK) return "Sweden Krona";
            if (c == Currency.SGD) return "Singapore Dollar";
            if (c == Currency.SHP) return "Saint Helena Pound";
            if (c == Currency.SLL) return "Sierra Leone Leone";
            if (c == Currency.SOS) return "Somalia Shilling";
            if (c == Currency.SPL) return "Seborga Luigino";
            if (c == Currency.SRD) return "Suriname Dollar";
            if (c == Currency.STN) return "São Tomé and Príncipe Dobra";
            if (c == Currency.SVC) return "El Salvador Colon";
            if (c == Currency.SYP) return "Syria Pound";
            if (c == Currency.SZL) return "Swaziland Lilangeni";
            if (c == Currency.THB) return "Thailand Baht";
            if (c == Currency.TJS) return "Tajikistan Somoni";
            if (c == Currency.TMT) return "Turkmenistan Manat";
            if (c == Currency.TND) return "Tunisia Dinar";
            if (c == Currency.TOP) return "Tonga Pa'anga";
            if (c == Currency.TRY) return "Turkey Lira";
            if (c == Currency.TTD) return "Trinidad and Tobago Dollar";
            if (c == Currency.TVD) return "Tuvalu Dollar";
            if (c == Currency.TWD) return "Taiwan New Dollar";
            if (c == Currency.TZS) return "Tanzania Shilling";
            if (c == Currency.UAH) return "Ukraine Hryvnia";
            if (c == Currency.UGX) return "Uganda Shilling";
            if (c == Currency.USD) return "United States Dollar";
            if (c == Currency.UYU) return "Uruguay Peso";
            if (c == Currency.UZS) return "Uzbekistan Som";
            if (c == Currency.VEF) return "Venezuela Bolívar";
            if (c == Currency.VND) return "Viet Nam Dong";
            if (c == Currency.VUV) return "Vanuatu Vatu";
            if (c == Currency.WST) return "Samoa Tala";
            if (c == Currency.XAF) return "Communauté Financière Africaine (BEAC) CFA Franc BEAC";
            if (c == Currency.XCD) return "East Caribbean Dollar";
            if (c == Currency.XDR) return "International Monetary Fund (IMF) Special Drawing Rights";
            if (c == Currency.XOF) return "Communauté Financière Africaine (BCEAO) Franc";
            if (c == Currency.XPF) return "Comptoirs Français du Pacifique (CFP) Franc";
            if (c == Currency.YER) return "Yemen Rial";
            if (c == Currency.ZAR) return "South Africa Rand";
            if (c == Currency.ZMW) return "Zambia Kwacha";
            if (c == Currency.ZWD) return "Zimbabwe Dolla";
            return c.ToString();
        }
        public static string Symbol(this Currency c) {
            if (c == Currency.EUR) return "E";
            if (c == Currency.USD) return "$";
            if (c == Currency.JPY) return "Y";
            if (c == Currency.GBP) return "£";
            return c.ToString();
        }
    }


}
