import type { Metadata } from "next";
import { Plus_Jakarta_Sans, IBM_Plex_Mono } from "next/font/google";
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
  title: "ISS ERP",
  description: "ISS ERP System",
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
