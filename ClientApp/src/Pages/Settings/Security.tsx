import LoadingButton from '@/Components/Button/LoadingButton';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import MainLayout from '@/Layouts/MainLayout';
import { PageProps } from '@/types';
import { Head, Link, router, useForm, usePage } from '@inertiajs/react';
import { startRegistration } from '@simplewebauthn/browser';
import { QRCodeSVG } from 'qrcode.react';
import React, { useState } from 'react';

type Session = {
    id: number;
    ip_address: string | null;
    user_agent: string | null;
    last_activity_at: string;
    is_current: boolean;
};

type TwoFactorSetupData = {
    key: string;
    uri: string;
};

type SecurityProps = {
    sessions?: Session[];
    twoFactorEnabled?: boolean;
    recoveryCodesLeft?: number;
    recoveryCodes?: string[];
    twoFactorSetup?: TwoFactorSetupData;
    passkeys?: Passkey[];
};

type Passkey = {
    credential_id: string;
    name: string;
    created_at: string;
};

const Security = () => {
    const { auth } = usePage<PageProps>().props;
    const {
        sessions = [],
        twoFactorEnabled = false,
        recoveryCodesLeft = 0,
        recoveryCodes,
        twoFactorSetup,
        passkeys = [],
    } = usePage<PageProps & SecurityProps>().props;

    return (
        <div>
            <Head title="Security Settings" />

            <div className="mb-8 flex max-w-lg justify-start">
                <h1 className="text-3xl font-bold">
                    <Link
                        href="/settings/security"
                        className="text-indigo-600 hover:text-indigo-700"
                    >
                        Settings
                    </Link>
                    <span className="mx-2 font-medium text-indigo-600">/</span>
                    Security
                </h1>
            </div>

            <div className="space-y-8">
                <UpdatePasswordForm />
                <UpdateEmailForm currentEmail={auth.user.email} />
                <TwoFactorSection
                    enabled={twoFactorEnabled}
                    recoveryCodesLeft={recoveryCodesLeft}
                    recoveryCodes={recoveryCodes}
                    setupData={twoFactorSetup}
                />
                <PasskeysSection passkeys={passkeys} />
                <ActiveSessionsSection sessions={sessions} />
            </div>
        </div>
    );
};

// --- Update Password ---

function UpdatePasswordForm() {
    const {
        data,
        setData,
        errors,
        put,
        processing,
        reset,
        recentlySuccessful,
    } = useForm({
        current_password: '',
        password: '',
        password_confirmation: '',
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        put('/settings/security/password', {
            preserveScroll: true,
            onSuccess: () => reset(),
        });
    }

    return (
        <div className="max-w-xl overflow-hidden rounded bg-white shadow">
            <form onSubmit={handleSubmit}>
                <div className="space-y-6 p-8">
                    <div>
                        <h2 className="text-lg font-semibold text-gray-900">
                            Update Password
                        </h2>
                        <p className="mt-1 text-sm text-gray-600">
                            Ensure your account is using a long, random password
                            to stay secure.
                        </p>
                    </div>
                    <FieldGroup
                        label="Current Password"
                        name="current_password"
                        error={errors.current_password}
                    >
                        <TextInput
                            name="current_password"
                            type="password"
                            autoComplete="current-password"
                            error={errors.current_password}
                            value={data.current_password}
                            onChange={(e) =>
                                setData('current_password', e.target.value)
                            }
                        />
                    </FieldGroup>
                    <FieldGroup
                        label="New Password"
                        name="password"
                        error={errors.password}
                    >
                        <TextInput
                            name="password"
                            type="password"
                            autoComplete="new-password"
                            error={errors.password}
                            value={data.password}
                            onChange={(e) =>
                                setData('password', e.target.value)
                            }
                        />
                    </FieldGroup>
                    <FieldGroup
                        label="Confirm Password"
                        name="password_confirmation"
                        error={errors.password_confirmation}
                    >
                        <TextInput
                            name="password_confirmation"
                            type="password"
                            autoComplete="new-password"
                            error={errors.password_confirmation}
                            value={data.password_confirmation}
                            onChange={(e) =>
                                setData('password_confirmation', e.target.value)
                            }
                        />
                    </FieldGroup>
                </div>
                <div className="flex items-center border-t border-gray-200 bg-gray-100 px-8 py-4">
                    {recentlySuccessful && (
                        <span className="text-sm text-green-600">Saved.</span>
                    )}
                    <LoadingButton
                        loading={processing}
                        type="submit"
                        className="btn-indigo ml-auto"
                    >
                        Update Password
                    </LoadingButton>
                </div>
            </form>
        </div>
    );
}

// --- Update Email ---

function UpdateEmailForm({ currentEmail }: { currentEmail: string }) {
    const {
        data,
        setData,
        errors,
        put,
        processing,
        reset,
        recentlySuccessful,
    } = useForm({
        email: '',
        email_password: '',
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        put('/settings/security/email', {
            preserveScroll: true,
            onSuccess: () => reset(),
        });
    }

    return (
        <div className="max-w-xl overflow-hidden rounded bg-white shadow">
            <form onSubmit={handleSubmit}>
                <div className="space-y-6 p-8">
                    <div>
                        <h2 className="text-lg font-semibold text-gray-900">
                            Update Email Address
                        </h2>
                        <p className="mt-1 text-sm text-gray-600">
                            A confirmation link will be sent to your new email
                            address.
                        </p>
                    </div>
                    <div className="rounded-md bg-gray-50 px-4 py-3 text-sm text-gray-600">
                        Current email:{' '}
                        <span className="font-medium text-gray-900">
                            {currentEmail}
                        </span>
                    </div>
                    <FieldGroup
                        label="New Email Address"
                        name="email"
                        error={errors.email}
                    >
                        <TextInput
                            name="email"
                            type="email"
                            autoComplete="email"
                            placeholder="Enter new email address"
                            error={errors.email}
                            value={data.email}
                            onChange={(e) => setData('email', e.target.value)}
                        />
                    </FieldGroup>
                    <FieldGroup
                        label="Confirm Your Password"
                        name="email_password"
                        error={errors.email_password}
                    >
                        <TextInput
                            name="email_password"
                            type="password"
                            autoComplete="current-password"
                            placeholder="Enter your current password"
                            error={errors.email_password}
                            value={data.email_password}
                            onChange={(e) =>
                                setData('email_password', e.target.value)
                            }
                        />
                    </FieldGroup>
                </div>
                <div className="flex items-center border-t border-gray-200 bg-gray-100 px-8 py-4">
                    {recentlySuccessful && (
                        <span className="text-sm text-green-600">
                            Confirmation email sent.
                        </span>
                    )}
                    <LoadingButton
                        loading={processing}
                        type="submit"
                        className="btn-indigo ml-auto"
                    >
                        Send Confirmation
                    </LoadingButton>
                </div>
            </form>
        </div>
    );
}

// --- Two-Factor Authentication ---

function TwoFactorSection({
    enabled,
    recoveryCodesLeft,
    recoveryCodes,
    setupData,
}: {
    enabled: boolean;
    recoveryCodesLeft: number;
    recoveryCodes?: string[];
    setupData?: TwoFactorSetupData;
}) {
    const [showSetup, setShowSetup] = useState(!!setupData);
    const {
        data: verifyData,
        setData: setVerifyData,
        errors: verifyErrors,
        post: postVerify,
        processing: verifying,
    } = useForm({ code: '' });

    const {
        data: disableData,
        setData: setDisableData,
        errors: disableErrors,
        post: postDisable,
        processing: disabling,
    } = useForm({ password: '' });

    function startSetup() {
        router.post(
            '/settings/security/two-factor/setup',
            {},
            {
                preserveScroll: true,
                onSuccess: () => setShowSetup(true),
            },
        );
    }

    function enableTwoFactor(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        postVerify('/settings/security/two-factor/enable', {
            preserveScroll: true,
        });
    }

    function disableTwoFactor(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        postDisable('/settings/security/two-factor/disable', {
            preserveScroll: true,
        });
    }

    function regenerateCodes() {
        router.post(
            '/settings/security/two-factor/recovery-codes',
            {},
            {
                preserveScroll: true,
            },
        );
    }

    return (
        <div className="max-w-xl overflow-hidden rounded bg-white shadow">
            <div className="p-8">
                <div className="mb-6">
                    <h2 className="text-lg font-semibold text-gray-900">
                        Two-Factor Authentication
                    </h2>
                    <p className="mt-1 text-sm text-gray-600">
                        Add an extra layer of security using a TOTP
                        authenticator app.
                    </p>
                </div>

                {!enabled && !setupData && (
                    <button
                        type="button"
                        className="btn-indigo"
                        onClick={startSetup}
                    >
                        Enable Two-Factor Authentication
                    </button>
                )}

                {setupData && !enabled && (
                    <div className="space-y-6">
                        <p className="text-sm text-gray-600">
                            Scan this QR code with your authenticator app, then
                            enter the verification code below.
                        </p>
                        <div className="flex justify-center rounded-lg bg-white p-4">
                            <QRCodeSVG value={setupData.uri} size={200} />
                        </div>
                        <div className="rounded-md bg-gray-50 px-4 py-3">
                            <p className="mb-1 text-xs font-medium text-gray-500">
                                Or enter this key manually:
                            </p>
                            <code className="font-mono text-sm break-all text-gray-900">
                                {setupData.key}
                            </code>
                        </div>
                        <form onSubmit={enableTwoFactor}>
                            <FieldGroup
                                label="Verification Code"
                                name="code"
                                error={verifyErrors.code}
                            >
                                <TextInput
                                    name="code"
                                    inputMode="numeric"
                                    autoComplete="one-time-code"
                                    placeholder="Enter 6-digit code"
                                    error={verifyErrors.code}
                                    value={verifyData.code}
                                    onChange={(e) =>
                                        setVerifyData('code', e.target.value)
                                    }
                                />
                            </FieldGroup>
                            <div className="mt-4">
                                <LoadingButton
                                    loading={verifying}
                                    type="submit"
                                    className="btn-indigo"
                                >
                                    Verify & Enable
                                </LoadingButton>
                            </div>
                        </form>
                    </div>
                )}

                {enabled && (
                    <div className="space-y-6">
                        <div className="flex items-center gap-2 rounded-md bg-green-50 px-4 py-3 text-sm text-green-800">
                            <svg
                                className="h-5 w-5"
                                fill="currentColor"
                                viewBox="0 0 20 20"
                            >
                                <path
                                    fillRule="evenodd"
                                    d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z"
                                    clipRule="evenodd"
                                />
                            </svg>
                            Two-factor authentication is enabled.
                            {recoveryCodesLeft > 0 && (
                                <span className="text-green-600">
                                    ({recoveryCodesLeft} recovery codes
                                    remaining)
                                </span>
                            )}
                        </div>

                        {recoveryCodes && recoveryCodes.length > 0 && (
                            <div className="rounded-md border border-amber-200 bg-amber-50 p-4">
                                <p className="mb-2 text-sm font-medium text-amber-800">
                                    Save these recovery codes in a secure
                                    location:
                                </p>
                                <div className="grid grid-cols-2 gap-1 rounded bg-white p-3 font-mono text-sm">
                                    {recoveryCodes.map((code) => (
                                        <div key={code}>{code}</div>
                                    ))}
                                </div>
                            </div>
                        )}

                        <div className="flex gap-3">
                            <button
                                type="button"
                                className="rounded border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
                                onClick={regenerateCodes}
                            >
                                Regenerate Recovery Codes
                            </button>
                        </div>

                        <form
                            onSubmit={disableTwoFactor}
                            className="border-t border-gray-200 pt-6"
                        >
                            <p className="mb-4 text-sm text-gray-600">
                                To disable two-factor authentication, enter your
                                password.
                            </p>
                            <FieldGroup
                                label="Password"
                                name="password"
                                error={disableErrors.password}
                            >
                                <TextInput
                                    name="password"
                                    type="password"
                                    autoComplete="current-password"
                                    error={disableErrors.password}
                                    value={disableData.password}
                                    onChange={(e) =>
                                        setDisableData(
                                            'password',
                                            e.target.value,
                                        )
                                    }
                                />
                            </FieldGroup>
                            <div className="mt-4">
                                <LoadingButton
                                    loading={disabling}
                                    type="submit"
                                    className="rounded bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700"
                                >
                                    Disable Two-Factor Authentication
                                </LoadingButton>
                            </div>
                        </form>
                    </div>
                )}
            </div>
        </div>
    );
}

// --- Active Sessions ---

function ActiveSessionsSection({ sessions }: { sessions: Session[] }) {
    function parseDevice(ua: string | null): string {
        if (!ua) return 'Unknown device';
        if (ua.includes('Mobile')) return 'Mobile';
        if (ua.includes('Chrome')) return 'Chrome';
        if (ua.includes('Firefox')) return 'Firefox';
        if (ua.includes('Safari')) return 'Safari';
        if (ua.includes('Edge')) return 'Edge';
        return 'Browser';
    }

    function revokeSession(id: number) {
        if (confirm('Are you sure you want to revoke this session?')) {
            router.delete(`/settings/security/sessions/${id}`, {
                preserveScroll: true,
            });
        }
    }

    return (
        <div className="max-w-xl overflow-hidden rounded bg-white shadow">
            <div className="p-8">
                <div className="mb-6">
                    <h2 className="text-lg font-semibold text-gray-900">
                        Active Sessions
                    </h2>
                    <p className="mt-1 text-sm text-gray-600">
                        Manage your active sessions across devices. Revoke any
                        sessions you don't recognize.
                    </p>
                </div>

                {sessions.length === 0 ? (
                    <p className="text-sm text-gray-500">No active sessions.</p>
                ) : (
                    <div className="space-y-3">
                        {sessions.map((session) => (
                            <div
                                key={session.id}
                                className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3"
                            >
                                <div className="min-w-0 flex-1">
                                    <div className="flex items-center gap-2">
                                        <span className="text-sm font-medium text-gray-900">
                                            {parseDevice(session.user_agent)}
                                        </span>
                                        {session.is_current && (
                                            <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-800">
                                                Current
                                            </span>
                                        )}
                                    </div>
                                    <div className="mt-1 text-xs text-gray-500">
                                        {session.ip_address ?? 'Unknown IP'}
                                        {' · '}
                                        {new Date(
                                            session.last_activity_at,
                                        ).toLocaleDateString(undefined, {
                                            month: 'short',
                                            day: 'numeric',
                                            hour: '2-digit',
                                            minute: '2-digit',
                                        })}
                                    </div>
                                </div>
                                {!session.is_current && (
                                    <button
                                        type="button"
                                        className="ml-4 text-sm text-red-600 hover:text-red-800"
                                        onClick={() =>
                                            revokeSession(session.id)
                                        }
                                    >
                                        Revoke
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
}

// --- Passkeys ---

function PasskeysSection({ passkeys }: { passkeys: Passkey[] }) {
    const [adding, setAdding] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function addPasskey() {
        setAdding(true);
        setError(null);

        try {
            // Get creation options from server
            const optionsRes = await fetch(
                '/settings/security/passkeys/options',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-XSRF-TOKEN': getCsrfToken(),
                    },
                },
            );
            const optionsJson = await optionsRes.json();

            // Start WebAuthn registration ceremony
            const credential = await startRegistration({
                optionsJSON: optionsJson,
            });

            // Send credential to server
            const name =
                prompt('Name this passkey (e.g., "MacBook Touch ID"):') ||
                'My passkey';
            const storeRes = await fetch('/settings/security/passkeys', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-XSRF-TOKEN': getCsrfToken(),
                },
                body: JSON.stringify({
                    credentialJson: JSON.stringify(credential),
                    name,
                }),
            });

            if (!storeRes.ok) {
                const err = await storeRes.json();
                setError(err.error || 'Failed to add passkey.');
            } else {
                router.reload({ only: ['passkeys'] });
            }
        } catch (err: unknown) {
            if (err instanceof Error && err.name !== 'NotAllowedError') {
                setError(err.message);
            }
        } finally {
            setAdding(false);
        }
    }

    function removePasskey(credentialId: string) {
        if (confirm('Are you sure you want to remove this passkey?')) {
            router.delete(
                `/settings/security/passkeys/${encodeURIComponent(credentialId)}`,
                { preserveScroll: true },
            );
        }
    }

    return (
        <div className="max-w-xl overflow-hidden rounded bg-white shadow">
            <div className="p-8">
                <div className="mb-6">
                    <h2 className="text-lg font-semibold text-gray-900">
                        Passkeys
                    </h2>
                    <p className="mt-1 text-sm text-gray-600">
                        Passkeys let you sign in securely using biometrics or
                        your device's screen lock. No password needed.
                    </p>
                </div>

                {error && (
                    <div className="mb-4 rounded-md bg-red-50 px-4 py-3 text-sm text-red-800">
                        {error}
                    </div>
                )}

                {passkeys.length > 0 && (
                    <div className="mb-4 space-y-3">
                        {passkeys.map((pk) => (
                            <div
                                key={pk.credential_id}
                                className="flex items-center justify-between rounded-lg border border-gray-200 px-4 py-3"
                            >
                                <div>
                                    <span className="text-sm font-medium text-gray-900">
                                        {pk.name}
                                    </span>
                                    <div className="text-xs text-gray-500">
                                        Added{' '}
                                        {new Date(
                                            pk.created_at,
                                        ).toLocaleDateString()}
                                    </div>
                                </div>
                                <button
                                    type="button"
                                    className="text-sm text-red-600 hover:text-red-800"
                                    onClick={() =>
                                        removePasskey(pk.credential_id)
                                    }
                                >
                                    Remove
                                </button>
                            </div>
                        ))}
                    </div>
                )}

                <button
                    type="button"
                    className="btn-indigo"
                    onClick={addPasskey}
                    disabled={adding}
                >
                    {adding ? 'Adding...' : 'Add a Passkey'}
                </button>
            </div>
        </div>
    );
}

function getCsrfToken(): string {
    const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
    return match ? decodeURIComponent(match[1]) : '';
}

Security.layout = (page: React.ReactNode) => <MainLayout children={page} />;

export default Security;
