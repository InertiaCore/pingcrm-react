import LoadingButton from '@/Components/Button/LoadingButton';
import { CheckboxInput } from '@/Components/Form/CheckboxInput';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, Link, router, useForm } from '@inertiajs/react';
import { startAuthentication } from '@simplewebauthn/browser';
import React, { useState } from 'react';

export default function LoginPage() {
    const { data, setData, errors, post, processing } = useForm({
        email: 'johndoe@example.com',
        password: 'Secret1234',
        remember: true,
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        post('/login');
    }

    return (
        <GuestLayout>
            <Head title="Login" />

            <div className="card shadow-xl">
                <form onSubmit={handleSubmit}>
                    <div className="card-body space-y-6">
                        <div className="text-center">
                            <h1 className="text-2xl font-bold text-gray-900">
                                Welcome Back!
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                Sign in to your account to continue
                            </p>
                        </div>

                        <div className="space-y-4">
                            <FieldGroup
                                label="Email Address"
                                name="email"
                                error={errors.email}
                            >
                                <TextInput
                                    name="email"
                                    type="email"
                                    autoComplete="email"
                                    placeholder="Enter your email"
                                    error={errors.email}
                                    value={data.email}
                                    onChange={(e) =>
                                        setData('email', e.target.value)
                                    }
                                />
                            </FieldGroup>

                            <FieldGroup
                                label="Password"
                                name="password"
                                error={errors.password}
                            >
                                <TextInput
                                    name="password"
                                    type="password"
                                    autoComplete="current-password"
                                    placeholder="Enter your password"
                                    error={errors.password}
                                    value={data.password}
                                    onChange={(e) =>
                                        setData('password', e.target.value)
                                    }
                                />
                            </FieldGroup>

                            <FieldGroup>
                                <CheckboxInput
                                    label="Remember me"
                                    name="remember"
                                    id="remember"
                                    checked={data.remember}
                                    onChange={(e) =>
                                        setData('remember', e.target.checked)
                                    }
                                />
                            </FieldGroup>
                        </div>
                    </div>

                    <div className="card-footer flex items-center justify-between">
                        <Link
                            className="btn-link text-sm"
                            tabIndex={-1}
                            href="/forgot-password"
                        >
                            Forgot your password?
                        </Link>
                        <LoadingButton
                            type="submit"
                            loading={processing}
                            className="btn-indigo"
                        >
                            Sign In
                        </LoadingButton>
                    </div>
                </form>

                <div className="border-t border-gray-200 px-6 py-4">
                    <PasskeySignIn />
                </div>
            </div>

            <p className="mt-6 text-center text-sm text-white/70">
                Don't have an account?{' '}
                <Link
                    href="/register"
                    className="font-medium text-white underline hover:text-white/90"
                >
                    Register
                </Link>
            </p>
        </GuestLayout>
    );
}

function PasskeySignIn() {
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    async function signInWithPasskey() {
        setLoading(true);
        setError(null);

        try {
            const csrfToken = document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1];
            const headers: Record<string, string> = {
                'Content-Type': 'application/json',
            };
            if (csrfToken) {
                headers['X-XSRF-TOKEN'] = decodeURIComponent(csrfToken);
            }

            const optionsRes = await fetch('/login/passkey/options', {
                method: 'POST',
                headers,
            });
            const optionsJson = await optionsRes.json();

            const credential = await startAuthentication({
                optionsJSON: optionsJson,
            });

            const signInRes = await fetch('/login/passkey', {
                method: 'POST',
                headers,
                body: JSON.stringify({
                    credentialJson: JSON.stringify(credential),
                }),
            });

            if (signInRes.ok) {
                const data = await signInRes.json();
                router.visit(data.redirect || '/');
            } else {
                const err = await signInRes.json();
                setError(err.error || 'Passkey authentication failed.');
            }
        } catch (err: unknown) {
            if (err instanceof Error && err.name !== 'NotAllowedError') {
                setError(err.message);
            }
        } finally {
            setLoading(false);
        }
    }

    return (
        <div className="text-center">
            {error && <p className="mb-2 text-sm text-red-600">{error}</p>}
            <button
                type="button"
                className="w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
                onClick={signInWithPasskey}
                disabled={loading}
            >
                {loading ? 'Verifying...' : 'Sign in with a Passkey'}
            </button>
        </div>
    );
}
