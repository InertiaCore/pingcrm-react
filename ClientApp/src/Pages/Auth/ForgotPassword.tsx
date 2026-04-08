import LoadingButton from '@/Components/Button/LoadingButton';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, Link, useForm } from '@inertiajs/react';
import React from 'react';

export default function ForgotPasswordPage() {
    const { data, setData, errors, post, processing } = useForm({
        email: '',
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        post('/forgot-password');
    }

    return (
        <GuestLayout>
            <Head title="Forgot Password" />

            <div className="card shadow-xl">
                <form onSubmit={handleSubmit}>
                    <div className="card-body space-y-6">
                        <div className="text-center">
                            <h1 className="text-2xl font-bold text-gray-900">
                                Forgot Password?
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                Enter your email and we'll send you a reset
                                link.
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
                        </div>
                    </div>

                    <div className="card-footer flex items-center justify-between">
                        <Link className="btn-link text-sm" href="/login">
                            Back to login
                        </Link>
                        <LoadingButton
                            type="submit"
                            loading={processing}
                            className="btn-indigo"
                        >
                            Send Reset Link
                        </LoadingButton>
                    </div>
                </form>
            </div>
        </GuestLayout>
    );
}
