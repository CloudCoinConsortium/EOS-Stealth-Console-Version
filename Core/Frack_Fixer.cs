﻿using CloudCoinCore;
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CloudCoinCore
{
    public class Frack_Fixer
    {
        /* INSTANCE VARIABLES */
        private IFileSystem fileUtils;
        private int totalValueToBank;
        private int totalValueToFractured;
        private int totalValueToCounterfeit;
        private RAIDA raida;
        public bool continueExecution = true;
        public bool IsFixing = false;

        /* CONSTRUCTORS */
        public Frack_Fixer(IFileSystem fileUtils, int timeout)
        {

            this.fileUtils = fileUtils;
            raida = RAIDA.GetInstance();
            totalValueToBank = 0;
            totalValueToCounterfeit = 0;
            totalValueToFractured = 0;
        }//constructor

        public string fixOneGuidCorner(int raida_ID, CloudCoin cc, int corner, int[] trustedTriad)
        {
            //RAIDA raida = RAIDA.GetInstance();
            CoinUtils cu = new CoinUtils(cc);
            ProgressChangedEventArgs pge = new ProgressChangedEventArgs();

            /*1. WILL THE BROKEN RAIDA FIX? check to see if it has problems echo, detect, or fix. */
            if (raida.nodes[raida_ID].FailsFix || raida.nodes[raida_ID].FailsEcho || raida.nodes[raida_ID].FailsEcho)
            {
                Console.Out.WriteLine("RAIDA Fails Echo or Fix. Try again when RAIDA online.");
                pge.MajorProgressMessage = ("RAIDA Fails Echo or Fix. Try again when RAIDA online.");
                raida.OnLogRecieved(pge);
                return "RAIDA Fails Echo or Fix. Try again when RAIDA online.";
            }
            else
            {
                /*2. ARE ALL TRUSTED RAIDA IN THE CORNER READY TO HELP?*/

                if (!raida.nodes[trustedTriad[0]].FailsEcho || !raida.nodes[trustedTriad[0]].FailsDetect || !raida.nodes[trustedTriad[1]].FailsEcho || !!raida.nodes[trustedTriad[1]].FailsDetect || !raida.nodes[trustedTriad[2]].FailsEcho || !raida.nodes[trustedTriad[2]].FailsDetect)
                {
                    /*3. GET TICKETS AND UPDATE RAIDA STATUS TICKETS*/
                    string[] ans = { cc.an[trustedTriad[0]], cc.an[trustedTriad[1]], cc.an[trustedTriad[2]] };
                    raida.GetTickets(trustedTriad, ans, cc.nn, cc.sn, cu.getDenomination(), 3000);

                    /*4. ARE ALL TICKETS GOOD?*/
                    if (raida.nodes[trustedTriad[0]].HasTicket && raida.nodes[trustedTriad[1]].HasTicket && raida.nodes[trustedTriad[2]].HasTicket)
                    {
                        /*5.T YES, so REQUEST FIX*/
                        //DetectionAgent da = new DetectionAgent(raida_ID, 5000);
                        if (!continueExecution)
                        {
                            Debug.WriteLine("Aborting Fix ");
                            pge.MajorProgressMessage = ("Aborting Fix ");
                            raida.OnLogRecieved(pge);

                            return "Aborting for new operation";
                        }
                        Response fixResponse = RAIDA.GetInstance().nodes[raida_ID].Fix(trustedTriad, raida.nodes[trustedTriad[0]].Ticket, raida.nodes[trustedTriad[1]].Ticket, raida.nodes[trustedTriad[2]].Ticket, cc.an[raida_ID]).Result;
                        /*6. DID THE FIX WORK?*/
                        if (fixResponse.success)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Out.WriteLine("");
                            Console.Out.WriteLine("RAIDA" + raida_ID + " unfracked successfully.");

                            pge.MajorProgressMessage = "RAIDA" + raida_ID + " unfracked successfully.";
                            raida.OnLogRecieved(pge);
                            //CoreLogger.Log("RAIDA" + raida_ID + " unfracked successfully.");
                            Console.Out.WriteLine("");
                            Console.ForegroundColor = ConsoleColor.White;
                            return "RAIDA" + raida_ID + " unfracked successfully.";

                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Out.WriteLine("");
                            Console.Out.WriteLine("RAIDA failed to accept tickets on corner " + corner);
                            pge.MajorProgressMessage = "RAIDA failed to accept tickets on corner " + corner;
                            raida.OnLogRecieved(pge);
                            //CoreLogger.Log("RAIDA failed to accept tickets on corner " + corner);
                            Console.Out.WriteLine("");
                            Console.ForegroundColor = ConsoleColor.White;
                            return "RAIDA failed to accept tickets on corner " + corner;
                        }//end if fix respons was success or fail
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Out.WriteLine("");
                        Console.Out.WriteLine("Trusted servers failed to provide tickets for corner " + corner);
                        pge.MajorProgressMessage = "Trusted servers failed to provide tickets for corner " + corner;
                        raida.OnLogRecieved(pge);
                        //CoreLogger.Log("Trusted servers failed to provide tickets for corner " + corner);
                        Console.Out.WriteLine("");
                        Console.ForegroundColor = ConsoleColor.White;

                        return "Trusted servers failed to provide tickets for corner " + corner;//no three good tickets
                    }//end if all good
                }//end if trused triad will echo and detect (Detect is used to get ticket)

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine("");
                Console.Out.WriteLine("One or more of the trusted triad will not echo and detect.So not trying.");
                pge.MajorProgressMessage = "One or more of the trusted triad will not echo and detect.So not trying.";
                raida.OnLogRecieved(pge);

                //CoreLogger.Log("One or more of the trusted triad will not echo and detect.So not trying.");
                Console.Out.WriteLine("");
                Console.ForegroundColor = ConsoleColor.White;
                return "One or more of the trusted triad will not echo and detect. So not trying.";
            }//end if RAIDA fails to fix. 

        }//end fix one



        /* PUBLIC METHODS */
        public int[] FixAll()
        {
            IsFixing = true;
            continueExecution = true;
            int[] results = new int[3];
            String[] frackedFileNames = new DirectoryInfo(this.fileUtils.FrackedFolder).GetFiles().Select(o => o.Name).ToArray();


            CloudCoin frackedCC;

            ProgressChangedEventArgs pge = new ProgressChangedEventArgs();
            pge.MajorProgressMessage = "Starting Frack Fixing";
            raida.OnLogRecieved(pge);

            //CoinUtils cu = new CoinUtils(frackedCC);
            if (frackedFileNames.Length < 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Out.WriteLine("You have no fracked coins.");
                
                //CoreLogger.Log("You have no fracked coins.");
                Console.ForegroundColor = ConsoleColor.White;
            }//no coins to unfrack



            for (int i = 0; i < frackedFileNames.Length; i++)
            {
                if (!continueExecution)
                {
                    Debug.WriteLine("Aborting Fix 1");
                    break;
                }
                Console.WriteLine("Unfracking coin " + (i + 1) + " of " + frackedFileNames.Length);
                //ProgressChangedEventArgs pge = new ProgressChangedEventArgs();
                pge.MajorProgressMessage = "Unfracking coin " + (i + 1) + " of " + frackedFileNames.Length;
                raida.OnLogRecieved(pge);
                //CoreLogger.Log("UnFracking coin " + (i + 1) + " of " + frackedFileNames.Length);
                try
                {
                    frackedCC = fileUtils.LoadCoin(this.fileUtils.FrackedFolder + frackedFileNames[i]);
                    if (frackedCC == null)
                        throw new IOException();
                    CoinUtils cu = new CoinUtils(frackedCC);
                    String value = frackedCC.pown;
                    //  Console.WriteLine("Fracked Coin: ");
                    cu.consoleReport();

                    CoinUtils fixedCC = fixCoin(frackedCC); // Will attempt to unfrack the coin. 
                    if (!continueExecution)
                    {
                        Debug.WriteLine("Aborting Fix 2");
                        break;
                    }
                    cu.consoleReport();
                    switch (fixedCC.getFolder().ToLower())
                    {
                        case "bank":
                            this.totalValueToBank++;
                            this.fileUtils.overWrite(this.fileUtils.BankFolder, fixedCC.cc);
                            this.deleteCoin(this.fileUtils.FrackedFolder + frackedFileNames[i]);
                            Console.WriteLine("EOS Stealth was moved to Bank.");
                            pge.MajorProgressMessage = "EOS Stealth was moved to Bank.";
                            raida.OnLogRecieved(pge);

                            //CoreLogger.Log("EOS Stealth was moved to Bank.");
                            break;
                        case "counterfeit":
                            this.totalValueToCounterfeit++;
                            this.fileUtils.overWrite(this.fileUtils.CounterfeitFolder, fixedCC.cc);
                            this.deleteCoin(this.fileUtils.FrackedFolder + frackedFileNames[i]);
                            Console.WriteLine("EOS Stealth was moved to Trash.");
                            pge.MajorProgressMessage = "EOS Stealth was moved to Trash.";
                            raida.OnLogRecieved(pge);

                            //CoreLogger.Log("EOS Stealth was moved to Trash.");
                            break;
                        default://Move back to fracked folder
                            this.totalValueToFractured++;
                            this.deleteCoin(this.fileUtils.FrackedFolder + frackedFileNames[i]);
                            this.fileUtils.overWrite(this.fileUtils.FrackedFolder, fixedCC.cc);
                            Console.WriteLine("EOS Stealth was moved back to Fracked folder.");
                            pge.MajorProgressMessage = "EOS Stealth was moved back to Fracked folder.";
                            raida.OnLogRecieved(pge);

                            //CoreLogger.Log("EOS Stealth was moved back to Fraked folder.");
                            break;
                    }
                    // end switch on the place the coin will go 
                    Console.WriteLine("...................................");
                    pge.MajorProgressMessage = "...................................";
                    raida.OnLogRecieved(pge);

                    Console.WriteLine("");
                }
                catch (FileNotFoundException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex);
                    //CoreLogger.Log(ex.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (IOException ioex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ioex);
                    //CoreLogger.Log(ioex.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                } // end try catch
            }// end for each file name that is fracked

            results[0] = this.totalValueToBank;
            results[1] = this.totalValueToCounterfeit; // System.out.println("Counterfeit and Moved to trash: "+totalValueToCounterfeit);
            results[2] = this.totalValueToFractured; // System.out.println("Fracked and Moved to Fracked: "+ totalValueToFractured);
            IsFixing = false;
            continueExecution = true;
            pge.MajorProgressMessage = "Finished Frack Fixing.";
            raida.OnLogRecieved(pge);

            pge.MajorProgressMessage = "Fixed " + totalValueToBank + " EOS Stealth and moved them into Bank Folder";
            if (totalValueToBank > 0)
                raida.OnLogRecieved(pge);

            return results;
        }// end fix all

        // End select all file names in a folder
        public bool deleteCoin(String path)
        {
            bool deleted = false;

            // System.out.println("Deleteing Coin: "+path + this.fileName + extension);
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //  CoreLogger.Log(e.ToString());
            }
            return deleted;
        }//end delete coin


        public CoinUtils fixCoin(CloudCoin brokeCoin)
        {
            CoinUtils cu = new CoinUtils(brokeCoin);
            ProgressChangedEventArgs pge = new ProgressChangedEventArgs();
            /*0. RESET TICKETS IN RAIDA STATUS TO EMPTY*/

            //RAIDA_Status.resetTickets();
            RAIDA.GetInstance().nodes.ToList().ForEach(x => x.ResetTicket());

            /*0. RESET THE DETECTION to TRUE if it is a new COIN */
            RAIDA.GetInstance().nodes.ToList().ForEach(x => x.NewCoin());

            //RAIDA_Status.newCoin();

            cu.setAnsToPans();// Make sure we set the RAIDA to the cc ans and not new pans. 
            DateTime before = DateTime.Now;

            String fix_result = "";
            FixitHelper fixer;

            /*START*/
            /*1. PICK THE CORNER TO USE TO TRY TO FIX */
            int corner = 1;
            // For every guid, check to see if it is fractured
            for (int raida_ID = 0; raida_ID < 25; raida_ID++)
            {
                if (!continueExecution)
                {
                    Debug.Write("Stopping Execution");
                    return cu;
                }
                //  Console.WriteLine("Past Status for " + raida_ID + ", " + brokeCoin.pastStatus[raida_ID]);

                if (cu.getPastStatus(raida_ID).ToLower() != "pass")//will try to fix everything that is not perfect pass.
                {

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Out.WriteLine("");
                    Console.Write("Attempting to fix RAIDA " + raida_ID);
                    pge.MajorProgressMessage = "Attempting to fix RAIDA " + raida_ID;
                    raida.OnLogRecieved(pge);
                    // CoreLogger.Log("Attempting to fix RAIDA " + raida_ID);
                    Console.Out.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.White;

                    fixer = new FixitHelper(raida_ID, brokeCoin.an.ToArray());

                    //trustedServerAns = new String[] { brokeCoin.ans[fixer.currentTriad[0]], brokeCoin.ans[fixer.currentTriad[1]], brokeCoin.ans[fixer.currentTriad[2]] };
                    corner = 1;
                    while (!fixer.finished)
                    {
                        if (!continueExecution)
                        {
                            Debug.Write("Stopping Execution");
                            return cu;
                        }
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(" Using corner " + corner + " Pown is "+ brokeCoin.pown);
                        pge.MajorProgressMessage = " Using corner " + corner;
                        raida.OnLogRecieved(pge);
                        //   CoreLogger.Log(" Using corner " + corner);
                        fix_result = fixOneGuidCorner(raida_ID, brokeCoin, corner, fixer.currentTriad);
                        // Console.WriteLine(" fix_result: " + fix_result + " for corner " + corner);
                        if (fix_result.Contains("success"))
                        {
                            //Fixed. Do the fixed stuff
                            cu.setPastStatus("pass", raida_ID);
                            fixer.finished = true;
                            corner = 1;
                        }
                        else
                        {
                            //Still broken, do the broken stuff. 
                            corner++;
                            fixer.setCornerToCheck(corner);
                        }
                    }//End whild fixer not finnished
                }//end if RAIDA past status is passed and does not need to be fixed
            }//end for each AN

            for (int raida_ID = 24; raida_ID > 0; raida_ID--)
            {
                //  Console.WriteLine("Past Status for " + raida_ID + ", " + brokeCoin.pastStatus[raida_ID]);
                if (!continueExecution)
                {
                    return cu;
                }

                if (cu.getPastStatus(raida_ID).ToLower() != "pass")//will try to fix everything that is not perfect pass.
                {

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Out.WriteLine("");
                    Console.WriteLine("Attempting to fix RAIDA " + raida_ID);
                    pge.MajorProgressMessage = "Attempting to fix RAIDA " + raida_ID;
                    raida.OnLogRecieved(pge);
                    //  CoreLogger.Log("Attempting to fix RAIDA " + raida_ID);
                    Console.Out.WriteLine("");
                    Console.ForegroundColor = ConsoleColor.White;

                    fixer = new FixitHelper(raida_ID, brokeCoin.an.ToArray());

                    //trustedServerAns = new String[] { brokeCoin.ans[fixer.currentTriad[0]], brokeCoin.ans[fixer.currentTriad[1]], brokeCoin.ans[fixer.currentTriad[2]] };
                    corner = 1;
                    while (!fixer.finished)
                    {
                        Console.WriteLine(" Using corner " + corner);
                        pge.MajorProgressMessage = " Using corner " + corner;
                        raida.OnLogRecieved(pge);
                        //CoreLogger.Log(" Using corner " + corner);
                        fix_result = fixOneGuidCorner(raida_ID, brokeCoin, corner, fixer.currentTriad);
                        // Console.WriteLine(" fix_result: " + fix_result + " for corner " + corner);
                        if (fix_result.Contains("success"))
                        {
                            //Fixed. Do the fixed stuff
                            cu.setPastStatus("pass", raida_ID);
                            fixer.finished = true;
                            corner = 1;
                        }
                        else
                        {
                            //Still broken, do the broken stuff. 
                            corner++;
                            fixer.setCornerToCheck(corner);
                        }
                    }//End whild fixer not finnished
                }//end if RAIDA past status is passed and does not need to be fixed
            }//end for each AN
            DateTime after = DateTime.Now;
            TimeSpan ts = after.Subtract(before);
            Console.WriteLine("Time spent fixing RAIDA in milliseconds: " + ts.Milliseconds);
            pge.MajorProgressMessage = "Time spent fixing RAIDA in milliseconds: " + ts.Milliseconds;
            raida.OnLogRecieved(pge);
            //CoreLogger.Log("Time spent fixing RAIDA in milliseconds: " + ts.Milliseconds);

            cu.calculateHP();//how many fails did it get
                             //  cu.gradeCoin();// sets the grade and figures out what the file extension should be (bank, fracked, counterfeit, lost

            cu.grade();
            cu.calcExpirationDate();
            return cu;
        }// end fix coin

    }//end class
}//end namespace