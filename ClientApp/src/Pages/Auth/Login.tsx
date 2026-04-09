import LoadingButton from '@/Components/Button/LoadingButton';
import { CheckboxInput } from '@/Components/Form/CheckboxInput';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, Link, useForm } from '@inertiajs/react';
import React from 'react';

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
