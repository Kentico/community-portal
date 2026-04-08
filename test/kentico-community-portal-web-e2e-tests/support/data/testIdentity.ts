export type RegistrationIdentity = {
  username: string;
  email: string;
  password: string;
  newPassword: string;
};

export function createRegistrationIdentity(
  prefix = "playwright-reg",
): RegistrationIdentity {
  const suffix = `${Date.now()}-${Math.floor(Math.random() * 10_000)}`;

  return {
    username: `${prefix}-${suffix}`,
    email: `${prefix}.${suffix}@kentico.local`,
    password: "Pass@12345",
    newPassword: "Pass@123456",
  };
}
