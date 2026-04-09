import LoadingButton from '@/Components/Button/LoadingButton';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, useForm } from '@inertiajs/react';
import React from 'react';

export default function VerifyEmailPage() {
    const { post, processing } = useForm({});

    function handleResend(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        post('/email/verification-notification');
    }

    return (
        <GuestLayout>
            <Head title="Verify Email" />

            <div className="card shadow-xl">
                <form onSubmit={handleResend}>
                    <div className="card-body space-y-6">
                        <div className="text-center">
                            <h1 className="text-2xl font-bold text-gray-900">
                                Verify Your Email
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                Thanks for signing up! Before getting started,
                                please verify your email address by clicking on
                                the link we just emailed to you.
                            </p>
                            <p className="mt-2 text-sm text-gray-600">
                                If you didn't receive the email, we'll gladly
                                send you another.
                            </p>
                        </div>
                    </div>

                    <div className="card-footer flex justify-end">
                        <LoadingButton
                            type="submit"
                            loading={processing}
                            className="btn-indigo"
                        >
                            Resend Verification Email
                        </LoadingButton>
                    </div>
                </form>
            </div>
        </GuestLayout>
    );
}
