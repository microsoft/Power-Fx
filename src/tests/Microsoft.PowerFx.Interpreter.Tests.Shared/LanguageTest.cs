// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.IR;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Functions;
using Microsoft.PowerFx.Interpreter;
using Microsoft.PowerFx.Interpreter.Tests;
using Microsoft.PowerFx.Types;
using Xunit;
using static Microsoft.PowerFx.Functions.Library;

namespace Microsoft.PowerFx.Tests
{
    public class LanguageTest : PowerFxTest
    {
        private static CultureInfo _defaultCulture;

        // Test language
        [Fact]
        public void GetLanguageTest()
        {
            var vnCulture = "vi-VN";
            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetCulture(new CultureInfo(vnCulture));
            runtimeConfig.SetTimeZone(TimeZoneInfo.Utc);
            var runner = new EvalVisitor(runtimeConfig, CancellationToken.None);

            var language = Language(runner, IRContext.NotInSource(FormulaType.String));
            Assert.Equal(vnCulture.ToLower(), language.Value.ToLower());
        }

        // Test default language
        [Fact]
        public void GetDefaultLanguageTest()
        {
            var engine = new RecalcEngine();
            var result = engine.Eval("Language()");

            Assert.Equal("en-US", result.ToObject());
        }

        [Fact]
        public void GetLanguageForNullCulture()
        {
            var runtimeConfig = new RuntimeConfig();
            var runner = new EvalVisitor(runtimeConfig, CancellationToken.None);

            var language = Language(runner, IRContext.NotInSource(FormulaType.String));
            Assert.Equal("en-US", language.Value);

            _defaultCulture = null;
            TestDefaultCulture(null);
        }

        [Fact]
        public void GetLanguageForInvariantCulture()
        {
            _defaultCulture = CultureInfo.InvariantCulture;
            ConfigTests.RunOnIsolatedThread(_defaultCulture, TestDefaultCulture);
        }

        [Theory]
        [InlineData("en-US", @"Text(1234.5678, ""#,##0.00"")", "1,234.57")]
        [InlineData("vi-VN", @"Text(1234.5678, ""#.##0,00"")", "1.234,57")]
        [InlineData("fr-FR", "Text(1234.5678, \"#\u202f##0,00\")", "1\u202F234,57")]
        [InlineData("fi-FI", "Text(1234.5678, \"#\u00A0##0,00\")", "1\u00A0234,57")]
        public async Task TextWithLanguageTest(string cultureName, string exp, string expectedResult)
        {
            var culture = new CultureInfo(cultureName);
            var recalcEngine = new RecalcEngine(new PowerFxConfig(Features.None));
            var symbols = new RuntimeConfig();
            symbols.SetCulture(culture);

            var result = await recalcEngine.EvalAsync(exp, CancellationToken.None, runtimeConfig: symbols);

            Assert.Equal(expectedResult, (result as StringValue).Value);
        }

        private void TestDefaultCulture(CultureInfo culture)
        {
            var engine = new RecalcEngine(new PowerFxConfig());
            var result = engine.Eval("Language()", options: new ParserOptions(culture));

            Assert.Equal("en-US", result.ToObject());
        }

        [Fact]
        public async Task TestTextInFrench()
        {
            var fr = new CultureInfo("fr-FR");
            fr.NumberFormat.NumberGroupSeparator = "\u00A0";
            fr = CultureInfo.ReadOnly(fr);

            var parserOptions = new ParserOptions(fr);
            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetCulture(fr);

            var engine = new RecalcEngine(new PowerFxConfig());
            FormulaValue result = await engine.EvalAsync("Text(5/2)", CancellationToken.None, options: parserOptions, runtimeConfig: runtimeConfig);

            Assert.IsNotType<ErrorValue>(result);
            Assert.IsType<StringValue>(result);
            Assert.Equal("2,5", result.ToObject());
        }

        [Theory]
        [InlineData(
            "bg-BG",
            "януари,февруари,март,април,май,юни,юли,август,септември,октомври,ноември,декември",
            "яну,фев,март,апр,май,юни,юли,авг,сеп,окт,ное,дек",
            "неделя,понеделник,вторник,сряда,четвъртък,петък,събота",
            "нд,пн,вт,ср,чт,пт,сб")]
        [InlineData(
            "ca-ES",
            "gener,febrer,març,abril,maig,juny,juliol,agost,setembre,octubre,novembre,desembre",
            "gen,febr,març,abr,maig,juny,jul,ag,set,oct,nov,des",
            "diumenge,dilluns,dimarts,dimecres,dijous,divendres,dissabte",
            "dg,dl,dt,dc,dj,dv,ds")]
        [InlineData(
            "cs-CZ",
            "leden,únor,březen,duben,květen,červen,červenec,srpen,září,říjen,listopad,prosinec",
            "led,úno,bře,dub,kvě,čvn,čvc,srp,zář,říj,lis,pro",
            "neděle,pondělí,úterý,středa,čtvrtek,pátek,sobota",
            "ne,po,út,st,čt,pá,so")]
        [InlineData(
            "da-DK",
            "januar,februar,marts,april,maj,juni,juli,august,september,oktober,november,december",
            "jan,feb,mar,apr,maj,jun,jul,aug,sep,okt,nov,dec",
            "søndag,mandag,tirsdag,onsdag,torsdag,fredag,lørdag",
            "søn,man,tir,ons,tor,fre,lør")]
        [InlineData(
            "de-DE",
            "Januar,Februar,März,April,Mai,Juni,Juli,August,September,Oktober,November,Dezember",
            "Jan,Feb,Mär,Apr,Mai,Jun,Jul,Aug,Sep,Okt,Nov,Dez",
            "Sonntag,Montag,Dienstag,Mittwoch,Donnerstag,Freitag,Samstag",
            "So,Mo,Di,Mi,Do,Fr,Sa")]
        [InlineData(
            "el-GR",
            "Ιανουάριος,Φεβρουάριος,Μάρτιος,Απρίλιος,Μάιος,Ιούνιος,Ιούλιος,Αύγουστος,Σεπτέμβριος,Οκτώβριος,Νοέμβριος,Δεκέμβριος",
            "Ιαν,Φεβ,Μάρ,Απρ,Μάι,Ιούν,Ιούλ,Αύγ,Σεπ,Οκτ,Νοέ,Δεκ",
            "Κυριακή,Δευτέρα,Τρίτη,Τετάρτη,Πέμπτη,Παρασκευή,Σάββατο",
            "Κυρ,Δευ,Τρί,Τετ,Πέμ,Παρ,Σάβ")]
        [InlineData(
            "en-US",
            "January,February,March,April,May,June,July,August,September,October,November,December",
            "Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sep,Oct,Nov,Dec",
            "Sunday,Monday,Tuesday,Wednesday,Thursday,Friday,Saturday",
            "Sun,Mon,Tue,Wed,Thu,Fri,Sat")]
        [InlineData(
            "es-ES",
            "enero,febrero,marzo,abril,mayo,junio,julio,agosto,septiembre,octubre,noviembre,diciembre",
            "ene,feb,mar,abr,may,jun,jul,ago,sept,oct,nov,dic",
            "domingo,lunes,martes,miércoles,jueves,viernes,sábado",
            "dom,lun,mar,mié,jue,vie,sáb")]
        [InlineData(
            "et-EE",
            "jaanuar,veebruar,märts,aprill,mai,juuni,juuli,august,september,oktoober,november,detsember",
            "jaan,veebr,märts,apr,mai,juuni,juuli,aug,sept,okt,nov,dets",
            "pühapäev,esmaspäev,teisipäev,kolmapäev,neljapäev,reede,laupäev",
            "P,E,T,K,N,R,L")]
        [InlineData(
            "eu-ES",
            "urtarrila,otsaila,martxoa,apirila,maiatza,ekaina,uztaila,abuztua,iraila,urria,azaroa,abendua",
            "urt,ots,mar,api,mai,eka,uzt,abu,ira,urr,aza,abe",
            "igandea,astelehena,asteartea,asteazkena,osteguna,ostirala,larunbata",
            "ig,al,ar,az,og,or,lr")]
        [InlineData(
            "fi-FI",
            "tammikuu,helmikuu,maaliskuu,huhtikuu,toukokuu,kesäkuu,heinäkuu,elokuu,syyskuu,lokakuu,marraskuu,joulukuu",
            "tammi,helmi,maalis,huhti,touko,kesä,heinä,elo,syys,loka,marras,joulu",
            "sunnuntai,maanantai,tiistai,keskiviikko,torstai,perjantai,lauantai",
            "su,ma,ti,ke,to,pe,la")]
        [InlineData(
            "fr-FR",
            "janvier,février,mars,avril,mai,juin,juillet,août,septembre,octobre,novembre,décembre",
            "janv,févr,mars,avr,mai,juin,juil,août,sept,oct,nov,déc",
            "dimanche,lundi,mardi,mercredi,jeudi,vendredi,samedi",
            "dim,lun,mar,mer,jeu,ven,sam")]
        [InlineData(
            "gl-ES",
            "Xaneiro,Febreiro,Marzo,Abril,Maio,Xuño,Xullo,Agosto,Setembro,Outubro,Novembro,Decembro",
            "Xan,Feb,Mar,Abr,Maio,Xuño,Xul,Ago,Set,Out,Nov,Dec",
            "Domingo,Luns,Martes,Mércores,Xoves,Venres,Sábado",
            "Dom,Luns,Mar,Mér,Xov,Ven,Sáb")]
        [InlineData(
            "hi-IN",
            "जनवरी,फ़रवरी,मार्च,अप्रैल,मई,जून,जुलाई,अगस्त,सितंबर,अक्तूबर,नवंबर,दिसंबर",
            "जन॰,फ़र॰,मार्च,अप्रैल,मई,जून,जुल॰,अग॰,सित॰,अक्तू॰,नव॰,दिस॰",
            "रविवार,सोमवार,मंगलवार,बुधवार,गुरुवार,शुक्रवार,शनिवार",
            "रवि,सोम,मंगल,बुध,गुरु,शुक्र,शनि")]
        [InlineData(
            "hr-HR",
            "siječanj,veljača,ožujak,travanj,svibanj,lipanj,srpanj,kolovoz,rujan,listopad,studeni,prosinac",
            "sij,velj,ožu,tra,svi,lip,srp,kol,ruj,lis,stu,pro",
            "nedjelja,ponedjeljak,utorak,srijeda,četvrtak,petak,subota",
            "ned,pon,uto,sri,čet,pet,sub")]
        [InlineData(
            "hu-HU",
            "január,február,március,április,május,június,július,augusztus,szeptember,október,november,december",
            "jan,febr,márc,ápr,máj,jún,júl,aug,szept,okt,nov,dec",
            "vasárnap,hétfő,kedd,szerda,csütörtök,péntek,szombat",
            "V,H,K,Sze,Cs,P,Szo")]
        [InlineData(
            "id-ID",
            "Januari,Februari,Maret,April,Mei,Juni,Juli,Agustus,September,Oktober,November,Desember",
            "Jan,Feb,Mar,Apr,Mei,Jun,Jul,Agu,Sep,Okt,Nov,Des",
            "Minggu,Senin,Selasa,Rabu,Kamis,Jumat,Sabtu",
            "Min,Sen,Sel,Rab,Kam,Jum,Sab")]
        [InlineData(
            "it-IT",
            "gennaio,febbraio,marzo,aprile,maggio,giugno,luglio,agosto,settembre,ottobre,novembre,dicembre",
            "gen,feb,mar,apr,mag,giu,lug,ago,set,ott,nov,dic",
            "domenica,lunedì,martedì,mercoledì,giovedì,venerdì,sabato",
            "dom,lun,mar,mer,gio,ven,sab")]
        [InlineData(
            "ja-JP",
            "1月,2月,3月,4月,5月,6月,7月,8月,9月,10月,11月,12月",
            "1月,2月,3月,4月,5月,6月,7月,8月,9月,10月,11月,12月",
            "日曜日,月曜日,火曜日,水曜日,木曜日,金曜日,土曜日",
            "日,月,火,水,木,金,土")]
        [InlineData(
            "kk-KZ",
            "Қаңтар,Ақпан,Наурыз,Сәуір,Мамыр,Маусым,Шілде,Тамыз,Қыркүйек,Қазан,Қараша,Желтоқсан",
            "қаң,ақп,нау,сәу,мам,мау,шіл,там,қыр,қаз,қар,жел",
            "жексенбі,дүйсенбі,сейсенбі,сәрсенбі,бейсенбі,жұма,сенбі",
            "жс,дс,сс,ср,бс,жм,сб")]
        [InlineData(
            "ko-KR",
            "1월,2월,3월,4월,5월,6월,7월,8월,9월,10월,11월,12월",
            "1월,2월,3월,4월,5월,6월,7월,8월,9월,10월,11월,12월",
            "일요일,월요일,화요일,수요일,목요일,금요일,토요일",
            "일,월,화,수,목,금,토")]
        [InlineData(
            "lt-LT",
            "sausis,vasaris,kovas,balandis,gegužė,birželis,liepa,rugpjūtis,rugsėjis,spalis,lapkritis,gruodis",
            "saus,vas,kov,bal,geg,birž,liep,rugp,rugs,spal,lapkr,gruod",
            "sekmadienis,pirmadienis,antradienis,trečiadienis,ketvirtadienis,penktadienis,šeštadienis",
            "sk,pr,an,tr,kt,pn,št")]
        [InlineData(
            "lv-LV",
            "janvāris,februāris,marts,aprīlis,maijs,jūnijs,jūlijs,augusts,septembris,oktobris,novembris,decembris",
            "janv,febr,marts,apr,maijs,jūn,jūl,aug,sept,okt,nov,dec",
            "Svētdiena,Pirmdiena,Otrdiena,Trešdiena,Ceturtdiena,Piektdiena,Sestdiena",
            "Svētd,Pirmd,Otrd,Trešd,Ceturtd,Piektd,Sestd")]
        [InlineData(
            "ms-MY",
            "Januari,Februari,Mac,April,Mei,Jun,Julai,Ogos,September,Oktober,November,Disember",
            "Jan,Feb,Mac,Apr,Mei,Jun,Jul,Ogo,Sep,Okt,Nov,Dis",
            "Ahad,Isnin,Selasa,Rabu,Khamis,Jumaat,Sabtu",
            "Ahd,Isn,Sel,Rab,Kha,Jum,Sab")]
        [InlineData(
            "nb-NO",
            "januar,februar,mars,april,mai,juni,juli,august,september,oktober,november,desember",
            "jan,feb,mar,apr,mai,jun,jul,aug,sep,okt,nov,des",
            "søndag,mandag,tirsdag,onsdag,torsdag,fredag,lørdag",
            "søn,man,tir,ons,tor,fre,lør")]
        [InlineData(
            "nl-NL",
            "januari,februari,maart,april,mei,juni,juli,augustus,september,oktober,november,december",
            "jan,feb,mrt,apr,mei,jun,jul,aug,sep,okt,nov,dec",
            "zondag,maandag,dinsdag,woensdag,donderdag,vrijdag,zaterdag",
            "zo,ma,di,wo,do,vr,za")]
        [InlineData(
            "pl-PL",
            "styczeń,luty,marzec,kwiecień,maj,czerwiec,lipiec,sierpień,wrzesień,październik,listopad,grudzień",
            "sty,lut,mar,kwi,maj,cze,lip,sie,wrz,paź,lis,gru",
            "niedziela,poniedziałek,wtorek,środa,czwartek,piątek,sobota",
            "niedz,pon,wt,śr,czw,pt,sob")]
        [InlineData(
            "pt-BR",
            "janeiro,fevereiro,março,abril,maio,junho,julho,agosto,setembro,outubro,novembro,dezembro",
            "jan,fev,mar,abr,mai,jun,jul,ago,set,out,nov,dez",
            "domingo,segunda-feira,terça-feira,quarta-feira,quinta-feira,sexta-feira,sábado",
            "dom,seg,ter,qua,qui,sex,sáb")]
        [InlineData(
            "pt-PT",
            "janeiro,fevereiro,março,abril,maio,junho,julho,agosto,setembro,outubro,novembro,dezembro",
            "jan,fev,mar,abr,mai,jun,jul,ago,set,out,nov,dez",
            "domingo,segunda-feira,terça-feira,quarta-feira,quinta-feira,sexta-feira,sábado",
            "domingo,segunda,terça,quarta,quinta,sexta,sábado")]
        [InlineData(
            "ro-RO",
            "ianuarie,februarie,martie,aprilie,mai,iunie,iulie,august,septembrie,octombrie,noiembrie,decembrie",
            "ian,feb,mar,apr,mai,iun,iul,aug,sept,oct,nov,dec",
            "duminică,luni,marți,miercuri,joi,vineri,sâmbătă",
            "dum,lun,mar,mie,joi,vin,sâm")]
        [InlineData(
            "ru-RU",
            "январь,февраль,март,апрель,май,июнь,июль,август,сентябрь,октябрь,ноябрь,декабрь",
            "янв,февр,март,апр,май,июнь,июль,авг,сент,окт,нояб,дек",
            "воскресенье,понедельник,вторник,среда,четверг,пятница,суббота",
            "вс,пн,вт,ср,чт,пт,сб")]
        [InlineData(
            "sk-SK",
            "január,február,marec,apríl,máj,jún,júl,august,september,október,november,december",
            "jan,feb,mar,apr,máj,jún,júl,aug,sep,okt,nov,dec",
            "nedeľa,pondelok,utorok,streda,štvrtok,piatok,sobota",
            "ne,po,ut,st,št,pi,so")]
        [InlineData(
            "sl-SI",
            "januar,februar,marec,april,maj,junij,julij,avgust,september,oktober,november,december",
            "jan,feb,mar,apr,maj,jun,jul,avg,sep,okt,nov,dec",
            "nedelja,ponedeljek,torek,sreda,četrtek,petek,sobota",
            "ned,pon,tor,sre,čet,pet,sob")]
        [InlineData(
            "sr-Cyrl-RS",
            "јануар,фебруар,март,април,мај,јун,јул,август,септембар,октобар,новембар,децембар",
            "јан,феб,мар,апр,мај,јун,јул,авг,сеп,окт,нов,дец",
            "недеља,понедељак,уторак,среда,четвртак,петак,субота",
            "нед,пон,уто,сре,чет,пет,суб")]
        [InlineData(
            "sr-Latn-RS",
            "januar,februar,mart,april,maj,jun,jul,avgust,septembar,oktobar,novembar,decembar",
            "jan,feb,mar,apr,maj,jun,jul,avg,sep,okt,nov,dec",
            "nedelja,ponedeljak,utorak,sreda,četvrtak,petak,subota",
            "ned,pon,uto,sre,čet,pet,sub")]
        [InlineData(
            "sv-SE",
            "januari,februari,mars,april,maj,juni,juli,augusti,september,oktober,november,december",
            "jan,feb,mars,apr,maj,juni,juli,aug,sep,okt,nov,dec",
            "söndag,måndag,tisdag,onsdag,torsdag,fredag,lördag",
            "sön,mån,tis,ons,tors,fre,lör")]
        [InlineData(
            "th-TH",
            "มกราคม,กุมภาพันธ์,มีนาคม,เมษายน,พฤษภาคม,มิถุนายน,กรกฎาคม,สิงหาคม,กันยายน,ตุลาคม,พฤศจิกายน,ธันวาคม",
            "ม.ค,ก.พ,มี.ค,เม.ย,พ.ค,มิ.ย,ก.ค,ส.ค,ก.ย,ต.ค,พ.ย,ธ.ค",
            "วันอาทิตย์,วันจันทร์,วันอังคาร,วันพุธ,วันพฤหัสบดี,วันศุกร์,วันเสาร์",
            "อา,จ,อ,พ,พฤ,ศ,ส")]
        [InlineData(
            "tr-TR",
            "Ocak,Şubat,Mart,Nisan,Mayıs,Haziran,Temmuz,Ağustos,Eylül,Ekim,Kasım,Aralık",
            "Oca,Şub,Mar,Nis,May,Haz,Tem,Ağu,Eyl,Eki,Kas,Ara",
            "Pazar,Pazartesi,Salı,Çarşamba,Perşembe,Cuma,Cumartesi",
            "Paz,Pzt,Sal,Çar,Per,Cum,Cmt")]
        [InlineData(
            "uk-UA",
            "січень,лютий,березень,квітень,травень,червень,липень,серпень,вересень,жовтень,листопад,грудень",
            "січ,лют,бер,кві,тра,чер,лип,сер,вер,жов,лис,гру",
            "неділя,понеділок,вівторок,середа,четвер,пʼятниця,субота",
            "нд,пн,вт,ср,чт,пт,сб")]
        [InlineData(
            "vi-VN",
            "Tháng 1,Tháng 2,Tháng 3,Tháng 4,Tháng 5,Tháng 6,Tháng 7,Tháng 8,Tháng 9,Tháng 10,Tháng 11,Tháng 12",
            "Thg 1,Thg 2,Thg 3,Thg 4,Thg 5,Thg 6,Thg 7,Thg 8,Thg 9,Thg 10,Thg 11,Thg 12",
            "Chủ Nhật,Thứ Hai,Thứ Ba,Thứ Tư,Thứ Năm,Thứ Sáu,Thứ Bảy",
            "CN,Th 2,Th 3,Th 4,Th 5,Th 6,Th 7")]
        [InlineData(
            "zh-CN",
            "一月,二月,三月,四月,五月,六月,七月,八月,九月,十月,十一月,十二月",
            "1月,2月,3月,4月,5月,6月,7月,8月,9月,10月,11月,12月",
            "星期日,星期一,星期二,星期三,星期四,星期五,星期六",
            "周日,周一,周二,周三,周四,周五,周六")]
        [InlineData(
            "zh-TW",
            "1月,2月,3月,4月,5月,6月,7月,8月,9月,10月,11月,12月",
            "1月,2月,3月,4月,5月,6月,7月,8月,9月,10月,11月,12月",
            "星期日,星期一,星期二,星期三,星期四,星期五,星期六",
            "週日,週一,週二,週三,週四,週五,週六")]
        public async Task TextCalendarFunctions(string cultureName, string monthsLong, string monthsShort, string weekdaysLong, string weekdaysShort)
        {
            var culture = new CultureInfo(cultureName);
            var recalcEngine = new RecalcEngine(new PowerFxConfig(Features.None));
            var runtimeConfig = new RuntimeConfig();
            runtimeConfig.SetCulture(culture);

            var monthsLongResult = await recalcEngine.EvalAsync("Calendar.MonthsLong()", CancellationToken.None, runtimeConfig: runtimeConfig);
            var monthsShortResult = await recalcEngine.EvalAsync("Calendar.MonthsShort()", CancellationToken.None, runtimeConfig: runtimeConfig);
            var weekdaysLongResult = await recalcEngine.EvalAsync("Calendar.WeekdaysLong()", CancellationToken.None, runtimeConfig: runtimeConfig);
            var weekdaysShortResult = await recalcEngine.EvalAsync("Calendar.WeekdaysShort()", CancellationToken.None, runtimeConfig: runtimeConfig);

            var tableResultToCSV = (FormulaValue result) =>
            {
                var tableResult = result as TableValue;
                var csvResult = string.Join(
                    ",",
                    tableResult.Rows
                        .Select(r => r.Value.GetField("Value"))
                        .Select(v => (v as StringValue).Value));
                return csvResult;
            };

            var allActualResults = string.Format(
                "{0}|{1}|{2}|{3}",
                tableResultToCSV(monthsLongResult),
                tableResultToCSV(monthsShortResult),
                tableResultToCSV(weekdaysLongResult),
                tableResultToCSV(weekdaysShortResult));
            var allExpectedResults = string.Format(
                "{0}|{1}|{2}|{3}",
                monthsLong,
                monthsShort,
                weekdaysLong,
                weekdaysShort);

            // Single comparison makes it easier to see all differences in the logs from the lab
            Assert.Equal(allExpectedResults, allActualResults);
        }
    }
}
