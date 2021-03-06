//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       thomass, timop
//
// Copyright 2004-2015 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;

using Ict.Testing.NUnitPetraServer;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.Interfaces.MSysMan;
using Ict.Petra.Server.MSysMan.ImportExport.WebConnectors;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Server.App.Core;
using Ict.Common.Remoting.Server;
using Ict.Common.Remoting.Shared;
using Ict.Common.Verification;
using Ict.Common;

namespace Ict.Petra.Tools.SampleDataConstructor
{
    /// <summary>
    /// This class creates sample data (partners, organisations, gifts) and imports them into OpenPetra.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class requires raw data to have been created already by benerator, reads this data, enhances
    /// and compiles it (using the literal meaning of "compile", i.e. putting together People, Addresses,
    /// Phonenumbers to create partners), and them imports this data to the OpenPetra Server.
    /// </para>
    /// <para>
    /// Generally, the Sample Data creator DOES NOT use the Petra Model internally,
    /// although it tries to stay close to it ( e.g. Naming Convention).
    /// This is so it can run a simple simulation for creating events (marriages resulting in same location, children, gift entries).
    /// These can then be saved in Petra.
    /// </para>
    ///
    /// </remarks>
    class TSampleDataConstructor
    {
        private enum eOperations : int
        {
            nothing = 0, importPartners = 1, importRecipients = 2, ledgerOneYear = 4, ledgerMultipleYears = 8, secondLedger = 16
        };

        // for the demo databases, we want to have an open period for the current month
        private static int CalculatedNumberOfClosedPeriods(int ANumberOfClosedYears)
        {
            if (DateTime.Today.Month <= 6)
            {
                return 12 * ANumberOfClosedYears + 11;
            }
            else
            {
                return 12 * ANumberOfClosedYears + 5;
            }
        }

        /// <summary>
        /// Creates Sample Data using the raw data provided and exports this to the Petra Server
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            TLogging.Log("Running Sample Data Constructor");

            try
            {
                TLogging.Log("(1) Initialize (check availability of resources, start the server)");

                TLogging.Log("\tStarting the server...");

                // use the config file defined on the command line with -C:
                TPetraServerConnector.Connect(string.Empty);

                // data directory containing the raw data files created by benerator
                string datadirectory = TAppSettingsManager.GetValue("dir.data.generated");

                eOperations operation = eOperations.importPartners | eOperations.importRecipients | eOperations.ledgerOneYear;

                if (TAppSettingsManager.GetValue("operation", false) == "secondLedger")
                {
                    operation = eOperations.secondLedger;
                }
                else if (TAppSettingsManager.GetValue("operation", false) == "ledgerMultipleYears")
                {
                    operation = eOperations.importPartners | eOperations.importRecipients | eOperations.ledgerMultipleYears;
                }

                if ((int)(operation & eOperations.importPartners) > 0)
                {
                    TLogging.Log("(2) Import partners");
                    SampleDataBankPartners.GenerateBanks(
                        Path.Combine(datadirectory, "banks.csv"));

                    SampleDataDonors.GenerateFamilyPartners(
                        Path.Combine(datadirectory, "people.csv"));

                    TLogging.Log("(3) Import organisations");
                    SampleDataOrganisations.GenerateOrganisationPartners(
                        Path.Combine(datadirectory, "organisations.csv"));
                }

                TLogging.Log("(4) Import recipients");

                if ((int)(operation & eOperations.importRecipients) > 0)
                {
                    // parse random data generated by benerator
                    SampleDataUnitPartners.GenerateFields(
                        Path.Combine(datadirectory, "fields.csv"));
                    SampleDataUnitPartners.GenerateKeyMinistries(
                        Path.Combine(datadirectory, "keymins.csv"));
                    SampleDataWorkers.GenerateWorkers(
                        Path.Combine(datadirectory, "workers.csv"));
                }

                if ((int)(operation & eOperations.ledgerOneYear) > 0)
                {
                    SampleDataLedger.FLedgerNumber = 43;
                    SampleDataLedger.FNumberOfClosedPeriods = CalculatedNumberOfClosedPeriods(0);
                    SampleDataLedger.InitCalendar();
                    SampleDataLedger.InitExchangeRate();
                    SampleDataLedger.PopulateData(datadirectory, true);
                    TLogging.Log("Please explicitely run nant importDemodata -D:operation=secondLedger");
                    TLogging.Log("   or                  nant importDemodata -D:operation=ledgerMultipleYears");
                }

                if ((int)(operation & eOperations.ledgerMultipleYears) > 0)
                {
                    SampleDataLedger.FLedgerNumber = 43;
                    SampleDataLedger.FNumberOfClosedPeriods = TAppSettingsManager.GetInt32("NumberOfClosedPeriods", CalculatedNumberOfClosedPeriods(2));
                    SampleDataLedger.InitCalendar();
                    SampleDataLedger.InitExchangeRate();
                    SampleDataLedger.PopulateData(datadirectory, true);
                }

                if ((int)(operation & eOperations.secondLedger) > 0)
                {
                    TLogging.Log("creating a second ledger");

                    // this ledger starts in period 4
                    SampleDataLedger.FLedgerNumber = 44;
                    SampleDataLedger.FNumberOfClosedPeriods = CalculatedNumberOfClosedPeriods(0) - 3;
                    SampleDataLedger.CreateNewLedger();
                    SampleDataLedger.InitExchangeRate();

                    SampleDataUnitPartners.FLedgerNumber = SampleDataLedger.FLedgerNumber;
                    SampleDataUnitPartners.GenerateFieldsFinanceOnly(
                        Path.Combine(datadirectory, "fields.csv"));

                    SampleDataLedger.PopulateData(datadirectory);
                }

                TLogging.Log("Completed.");
            }
            catch (Exception e)
            {
                TLogging.Log(e.Message);
                TLogging.Log(e.StackTrace);
                Environment.Exit(-1);
            }
        }
    }
}