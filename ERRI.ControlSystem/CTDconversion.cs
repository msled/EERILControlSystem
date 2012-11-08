using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EERIL.DeviceControls;

namespace EERIL.ControlSystem
{
    public partial class CTDconversion
    {
        public double TC0 = 130.903186300715;
        public double TC1 = -0.162379339146062;
        public double TC2 = 0.000133369043762939;
        public double TC3 = -6.90441745926269E-8;
        public double TC4 = 1.87668326618511E-11;
        public double TC5 = -2.15929868996701E-15;
        public double PC0 = -12.6747897535632;
        public double PC1 = 0.039533174066096;
        public double PC2 = 2.67957383127658E-7;
        public double PC3 = -3.12784371518367E-10;
        public double PC4 = 1.11775045123997E-13;
        public double PC5 = -1.3736102919566E-17;
        public double PtcC1 = -1.7672782735363;
        public double PtcC2 = 0.0495136366653886;
        public double PtcC3 = -0.00195036900807335;
        public double PtcC4 = 4.19776068393609E-5;
        public double PtcC5 = -3.73340626888235E-7;
        public double CC0 = 94.0650669475934;
        public double CC1 = -0.181340282484274;
        public double CC2 = 0.000157636863385389;
        public double CC3 = -6.76150130972562E-8;
        public double CC4 = 1.39350298793612E-11;
        public double CC5 = -1.10237642464623E-15;
        public double TCRC1 = 0.0374782903953379;
        public double TCRC2 = -0.140157562351192;
        public double TCRC3 = 0.0119903359597168;
        public double TCRC4 = -0.000359196003610852;
        public double TCRC5 = 3.57088429403337E-6;
        public double TCR1C1 = -1.5575111582994;
        public double TCR1C2 = 0.0386775040219753;
        public double TCR1C3 = -0.00603089800915985;
        public double TCR1C4 = 0.000222877425545097;
        public double TCR1C5 = -2.38878179891271E-6;
        public double a0 = 0.008;
        public double a1 = -0.1692;
        public double a2 = 25.3851;
        public double a3 = 14.0941;
        public double a4 = -7.0261;
        public double a5 = 2.7081;
        public double b0 = 0.0005;
        public double b1 = -0.0056;
        public double b2 = -0.0066;
        public double b3 = -0.0375;
        public double b4 = 0.0636;
        public double b5 = -0.0144;
        public double c0 = 0.6766097;
        public double c1 = 0.0200564;
        public double c2 = 0.0001104259;
        public double c3 = -0.00000069698;
        public double c4 = 0.0000000010031;
        public double Tpr = 22.86;
        public double Tcr = 23.86;
        public double T, P, C, Pc, Tv, Pv, Cv, A, B, L, H, Cc0, Cc1, Cdc;
        double[] values = new double[3];

        public double[] ConvertValues(double TL, double TH, double PL, double PH, double CL, double CH)
        {
            T = (TL) + (256 * TH);
            P = (PL) + (256 * PH);
            C = (CL) + (256 * CH);

            Tv = (TC0 + (TC1 * T) + (TC2 * Math.Pow(T, 2)) + (TC3 * Math.Pow(T, 3)) + (TC4 * Math.Pow(T, 4)) + (TC5 * Math.Pow(T, 5)));

            Pc = P + (PtcC1 * Tpr) + (PtcC2 * Math.Pow(Tpr, 2)) + (PtcC3 * Math.Pow(Tpr, 3)) + (PtcC4 * Math.Pow(Tpr, 4)) + (PtcC5 * Math.Pow(Tpr, 5)) - ((PtcC1 * Tv) + (PtcC2 * Math.Pow(Tv, 2)) + (PtcC3 * Math.Pow(Tv, 3)) + (PtcC4 * Math.Pow(Tv, 4)) + (PtcC5 * Math.Pow(Tv, 5)));
            Pv = (PC0 + (PC1 * Pc) + (PC2 * Math.Pow(Pc, 2)) + (PC3 * Math.Pow(Pc, 3)) + (PC4 * Math.Pow(Pc, 4)) + (PC5 * Math.Pow(Pc, 5)));

            Cc0 = C + (TCRC1 * Tcr) + (TCRC2 * Math.Pow(Tcr, 2)) + (TCRC3 * Math.Pow(Tcr, 3)) + (TCRC4 * Math.Pow(Tcr, 4)) + (TCRC5 * Math.Pow(Tcr, 5)) - ((TCR1C1 * Tv) + (TCR1C2 * Math.Pow(Tv, 2)) + (TCR1C3 * Math.Pow(Tv, 3)) + (TCR1C4 * Math.Pow(Tv, 4)) + (TCR1C5 * Math.Pow(Tv, 5)));
            Cc1 = C + (TCR1C1 * Tcr) + (TCR1C2 * Math.Pow(Tcr, 2)) + (TCR1C3 * Math.Pow(Tcr, 3)) + (TCR1C4 * Math.Pow(Tcr, 4)) + (TCR1C5 * Math.Pow(Tcr, 5)) - ((TCR1C1 * Tv) + (TCR1C2 * Math.Pow(Tv, 2)) + (TCR1C3 * Math.Pow(Tv, 3)) + (TCR1C4 * Math.Pow(Tv, 4)) + (TCR1C5 * Math.Pow(Tv, 5)));

            A = (Cc1 - Cc0) / (H - L);
            B = Cc0 - A * L;
            Cdc = B + (A * C);

            Cv = (float)(CC0 + (CC1 * Cdc) + (CC2 * Math.Pow(Cdc, 2)) + (CC3 * Math.Pow(Cdc, 3)) + (CC4 * Math.Pow(Cdc, 4)) + (CC5 * Math.Pow(Cdc, 5)));

            values[0] = Tv;
            values[1] = Pv;
            values[2] = Cv;

            return values;
        }
    }
}