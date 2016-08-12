using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElektronicznyNadawca
{
    class TestTools
    {
        
        public static ENLabs.errorType[] pobierzEtykietyZKoperty(EN client, int idEnvelope, out ENLabs.errorType[] errors)
        {
            System.Console.WriteLine("-----Pobieranie wszystkich etykiet z wysłanego bufora");
            byte[] etykiety = client.getAddresLabelCompact(idEnvelope, out errors);
            SaveData(@"etykiety-envelope-" + idEnvelope.ToString() + ".pdf", etykiety);

            return errors;
        }

        public static ENLabs.errorType[] pobierzPojedynczeEtykietyZKoperty(EN client, int idEnvelope, out ENLabs.errorType[] errors)
        {
            System.Console.WriteLine("-----Pobieranie pojedyńczych etykiet z wysłanego bufora");
            ENLabs.addressLabelContent[] etykiety;
            etykiety = client.getAddressLabel(idEnvelope, out errors);
            foreach (ENLabs.addressLabelContent etykieta in client.getAddressLabel(idEnvelope, out errors))
            {
                SaveData(@"etykiety-envelope-" + idEnvelope.ToString() + "-" + etykieta.guid + ".pdf", etykieta.pdfContent);
            }

            return errors;
        }

        public static void utworzNowyBufor(EN client, out ENLabs.errorType[] errors, ENLabs.profilType[] profiles, ENLabs.urzadNadaniaFullType[] urzedyNadania, out ENLabs.buforType bufor)
        {
            System.Console.Write("-----Towrzenie nowebo buforu: ");
            bufor = zdefiniujBufor(profiles.First(), urzedyNadania.First());
            errors = client.createEnvelopeBufor(bufor);
            System.Console.WriteLine(bufor.idBufor);
        }

        public static List<string> utworzPrzesylki(EN client, ENLabs.buforType bufor)
        {
            System.Console.WriteLine("-----Utworzenie przesyłek");
            List<ENLabs.przesylkaType> przesylki = new List<ENLabs.przesylkaType>();
            przesylki.Add(createPocztexUiszczaOplateNadawca());
            przesylki.Add(createPocztexUiszczaOplateNadawca());
            przesylki.Add(createPocztexBledna());
            List<string> guidy = nadajPrzesylki(client, bufor, przesylki);
            return guidy;
        }

        public static List<string> nadajPrzesylki(EN client, ENLabs.buforType bufor, List<ENLabs.przesylkaType> przesylki)
        {
            ENLabs.addShipmentResponseItemType[] przesylkiWyslane = client.addShipment(przesylki, bufor.idBufor);
            List<String> guidy = new List<string>();
            System.Console.WriteLine("-----Nadawanie przesyłek");
            foreach (ENLabs.addShipmentResponseItemType przesylka in przesylkiWyslane)
            {
                if (przesylka.error == null)
                {
                    System.Console.WriteLine("[" + przesylka.numerNadania + "] : " + przesylka.guid);
                    guidy.Add(przesylka.guid);
                }
                else
                {
                    System.Console.WriteLine("Przesyłka o guidzie " + przesylka.guid + " nie została nadana z powodu błędów");
                    foreach (ENLabs.errorType error in przesylka.error)
                    {
                        System.Console.WriteLine("[" + error.errorNumber + "] " + error.errorDesc);
                    }
                }
            }

            return guidy;
        }

        public static ENLabs.errorType[] pobierzEtykietyZBuforu(EN client, ENLabs.buforType bufor, List<string> guidy, out ENLabs.errorType[] errors)
        {
            System.Console.WriteLine("-----Pobranie etykiet");
            byte[] etykiety = client.getAddresLabelByGuidCompact(guidy.ToArray(), bufor.idBufor, out errors);
            SaveData(@"etykiety-bufor-" + bufor.idBufor.ToString() + ".pdf", etykiety);
            return errors;
        }

        public static ENLabs.urzadNadaniaFullType[] pobierzUrzedyNadaniaDlaKarty(EN c, ENLabs.kartaType karta)
        {
            System.Console.WriteLine("-----Pobranie urzędów nadania dla karty "+ karta.idKarta);
            ENLabs.urzadNadaniaFullType[] urzedyNadania = c.getUrzedyNadania();
            foreach (ENLabs.urzadNadaniaFullType urzadNadania in urzedyNadania)
            {
                System.Console.WriteLine("Urząd nadania dla karty " + karta.idKarta + " to " + urzadNadania.urzadNadania);
            }

            return urzedyNadania;
        }

        public static void ustawBiezacaKarte(EN c, ENLabs.kartaType karta)
        {
            c.setAktywnaKarta(karta.idKarta);
            System.Console.WriteLine("------Ustawiono bieżącą kartę na: " + karta.idKarta);
        }

        public static ENLabs.kartaType[] pobierzKartyZalogowanegoUzytkownika(EN c)
        {
            System.Console.Write("------Dostępne karty zalogowanego użytkownika:");
            var karty = c.getCurrentUserAccount().karta;
            foreach (ENLabs.kartaType karta in karty)
            {
                System.Console.Write(" " + karta.idKarta);
            }
            System.Console.WriteLine("");
            return karty;
        }

        public static ENLabs.profilType[] pobierzProfileZalogowanegoUzytkownika(EN c)
        {
            ENLabs.profilType[] profiles = c.getCurrentUserProfileList();
            System.Console.WriteLine("------Profile zalogowanego użytkownika");
            foreach (ENLabs.profilType profile in profiles)
            {
                System.Console.WriteLine(profile.idProfil + " " + profile.nazwaSkrocona);
            }

            return profiles;
        }

        public static ENLabs.buforType zdefiniujBufor(ENLabs.profilType profil, ENLabs.urzadNadaniaFullType urzadNadania)
        {
            ENLabs.buforType bufor = new ENLabs.buforType();
            bufor.dataNadania = DateTime.Now.AddDays(1);
            bufor.profil = profil;
            bufor.dataNadaniaSpecified = true;
            bufor.opis = "Nowy bufor webapi " + DateTime.Now.AddDays(1).ToString();
            bufor.urzadNadania = urzadNadania.urzadNadania;
            bufor.urzadNadaniaSpecified = true;
            return bufor;
        }

        public static ENLabs.przesylkaType createPocztexUiszczaOplateNadawca()
        {
            var pocztex = new ENLabs.uslugaKurierskaType();
            pocztex.adres = new ENLabs.adresType();
            pocztex.adres.nazwa = "Jan Kowalski";
            pocztex.adres.miejscowosc = "Gdynia";
            pocztex.adres.ulica = "10 Lutego";
            pocztex.adres.kodPocztowy = "81-301";

            pocztex.uiszczaOplate = ENLabs.uiszczaOplateType.NADAWCA;
            pocztex.uiszczaOplateSpecified = true;
            pocztex.termin = ENLabs.terminKurierskaType.EKSPRES24;
            pocztex.terminSpecified = true;
            var g = Guid.NewGuid();
            pocztex.guid = g.ToString();
            return pocztex;
        }

        public static ENLabs.przesylkaType createPocztexBledna()
        {
            var pocztex = new ENLabs.uslugaKurierskaType();
            pocztex.adres = new ENLabs.adresType();
            pocztex.adres.nazwa = "Jan Kowalski";
            pocztex.adres.miejscowosc = "Gdynia";
            pocztex.adres.ulica = "10 Lutego";
            pocztex.adres.kodPocztowy = "81-301";

            pocztex.uiszczaOplate = ENLabs.uiszczaOplateType.NADAWCA;
            pocztex.uiszczaOplateSpecified = true;
            pocztex.termin = ENLabs.terminKurierskaType.EKSPRES24;
            pocztex.terminSpecified = true;
            pocztex.guid = "123456";
            return pocztex;
        }

        public static bool SaveData(string fileNamePath, byte[] Data)
        {
            BinaryWriter Writer = null;
            try
            {
                Writer = new BinaryWriter(File.OpenWrite(fileNamePath));
                Writer.Write(Data);
                Writer.Flush();
            }
            catch
            {
                return false;
            }
            finally
            {
                Writer.Close();
            }

            return true;
        }
    }
}
