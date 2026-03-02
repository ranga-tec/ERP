import { cookies } from "next/headers";
import { parseUserSettings, USER_SETTINGS_COOKIE, type UserSettings } from "@/lib/user-settings";

export async function userSettingsFromCookies(): Promise<UserSettings> {
  const cookieStore = await cookies();
  const raw = cookieStore.get(USER_SETTINGS_COOKIE)?.value;
  return parseUserSettings(raw);
}
