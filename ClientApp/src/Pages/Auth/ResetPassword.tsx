import LoadingButton from '@/Components/Button/LoadingButton';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, useForm } from '@inertiajs/react';
import React from 'react';

interface ResetPasswordProps {
    token: string;
    email: string;
}

export default function ResetPasswordPage({
    token,
    email,
}: ResetPasswordProps) {
    const { data, setData, errors, post, processing } = useForm({
        token,
        email,
        password: '',
        password_confirmation: '',
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        post('/reset-password');
    }

    return (
        <GuestLayout>
            <Head title="Reset Password" />

            <div className="card shadow-xl">
                <form onSubmit={handleSubmit}>
                    <div className="card-body space-y-6">
                        <div className="text-center">
                            <h1 className="text-2xl font-bold text-gray-900">
                                Reset Password
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                Enter your new password below.
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
                                    error={errors.email}
                                    value={data.email}
                                    onChange={(e) =>
                                        setData('email', e.target.value)
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
                                    placeholder="Enter new password"
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
                                    placeholder="Confirm new password"
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

                    <div className="card-footer flex justify-end">
                        <LoadingButton
                            type="submit"
                            loading={processing}
                            className="btn-indigo"
                        >
                            Reset Password
                        </LoadingButton>
                    </div>
                </form>
            </div>
        </GuestLayout>
    );
}
