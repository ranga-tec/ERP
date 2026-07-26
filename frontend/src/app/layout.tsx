import type { Metadata } from "next";
import { Plus_Jakarta_Sans, IBM_Plex_Mono } from "next/font/google";
import { company } from "@/lib/company";
import { userSettingsThemeBootstrapScript } from "@/lib/user-settings";
import "./globals.css";

const appSans = Plus_Jakarta_Sans({
  variable: "--font-geist-sans",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});

const appMono = IBM_Plex_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
  weight: ["400", "500", "600"],
});

export const metadata: Metadata = {
  title: {
    default: `${company.shortName} ERP`,
    template: `%s · ${company.shortName} ERP`,
  },
  description: `Inventory, service, procurement, sales, finance, and reporting platform for ${company.name}.`,
  applicationName: `${company.shortName} ERP`,
  icons: {
    icon: [{ url: "/icon.png", type: "image/png" }],
    shortcut: "/favicon.ico",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <script
          dangerouslySetInnerHTML={{
            __html: userSettingsThemeBootstrapScript(),
          }}
        />
      </head>
      <body
        className={`${appSans.variable} ${appMono.variable} antialiased`}
      >
        {children}
      </body>
    </html>
  );
}
