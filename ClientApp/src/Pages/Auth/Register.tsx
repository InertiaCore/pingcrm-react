import LoadingButton from '@/Components/Button/LoadingButton';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, Link, useForm } from '@inertiajs/react';
import React from 'react';

export default function RegisterPage() {
    const { data, setData, errors, post, processing } = useForm({
        first_name: '',
        last_name: '',
        email: '',
        password: '',
        password_confirmation: '',
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        post('/register');
    }

    return (
        <GuestLayout>
            <Head title="Register" />

            <div className="card shadow-xl">
                <form onSubmit={handleSubmit}>
                    <div className="card-body space-y-6">
                        <div className="text-center">
                            <h1 className="text-2xl font-bold text-gray-900">
                                Create an Account
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                Sign up to get started with PingCRM
                            </p>
                        </div>

                        <div className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <FieldGroup
                                    label="First Name"
                                    name="first_name"
                                    error={errors.first_name}
                                >
                                    <TextInput
                                        name="first_name"
                                        autoComplete="given-name"
                                        placeholder="First name"
                                        error={errors.first_name}
                                        value={data.first_name}
                                        onChange={(e) =>
                                            setData(
                                                'first_name',
                                                e.target.value,
                                            )
                                        }
                                    />
                                </FieldGroup>

                                <FieldGroup
                                    label="Last Name"
                                    name="last_name"
                                    error={errors.last_name}
                                >
                                    <TextInput
                                        name="last_name"
                                        autoComplete="family-name"
                                        placeholder="Last name"
                                        error={errors.last_name}
                                        value={data.last_name}
                                        onChange={(e) =>
                                            setData('last_name', e.target.value)
                                        }
                                    />
                                </FieldGroup>
                            </div>

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
                                    autoComplete="new-password"
                                    placeholder="Min 10 characters"
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
                                    placeholder="Confirm your password"
                                    error={errors.password_confirmation}
                                    value={data.password_confirmation}
                                    onChange={(e) =>
                                        setData(
                                            'password_confirmation',
                                            e.target.value,
                                        )
                                    }
                                />
                            </FieldGroup>
                        </div>
                    </div>

                    <div className="card-footer flex items-center justify-between">
                        <Link className="btn-link text-sm" href="/login">
                            Already have an account?
                        </Link>
                        <LoadingButton
                            type="submit"
                            loading={processing}
                            className="btn-indigo"
                        >
                            Register
                        </LoadingButton>
                    </div>
                </form>
            </div>
        </GuestLayout>
    );
}
