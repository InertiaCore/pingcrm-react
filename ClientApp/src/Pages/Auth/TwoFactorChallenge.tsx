import LoadingButton from '@/Components/Button/LoadingButton';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import GuestLayout from '@/Layouts/GuestLayout';
import { Head, useForm } from '@inertiajs/react';
import React, { useState } from 'react';

export default function TwoFactorChallengePage() {
    const [useRecoveryCode, setUseRecoveryCode] = useState(false);

    const { data, setData, errors, post, processing } = useForm({
        code: '',
        recovery_code: '',
    });

    function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();
        post('/two-factor-challenge');
    }

    return (
        <GuestLayout>
            <Head title="Two-Factor Challenge" />

            <div className="card shadow-xl">
                <form onSubmit={handleSubmit}>
                    <div className="card-body space-y-6">
                        <div className="text-center">
                            <h1 className="text-2xl font-bold text-gray-900">
                                Two-Factor Authentication
                            </h1>
                            <p className="mt-2 text-sm text-gray-600">
                                {useRecoveryCode
                                    ? 'Enter one of your recovery codes to continue.'
                                    : 'Enter the code from your authenticator app to continue.'}
                            </p>
                        </div>

                        <div className="space-y-4">
                            {useRecoveryCode ? (
                                <FieldGroup
                                    label="Recovery Code"
                                    name="recovery_code"
                                    error={errors.code}
                                >
                                    <TextInput
                                        name="recovery_code"
                                        autoComplete="one-time-code"
                                        placeholder="Enter recovery code"
                                        error={errors.code}
                                        value={data.recovery_code}
                                        onChange={(e) =>
                                            setData(
                                                'recovery_code',
                                                e.target.value,
                                            )
                                        }
                                    />
                                </FieldGroup>
                            ) : (
                                <FieldGroup
                                    label="Authentication Code"
                                    name="code"
                                    error={errors.code}
                                >
                                    <TextInput
                                        name="code"
                                        inputMode="numeric"
                                        autoComplete="one-time-code"
                                        placeholder="Enter 6-digit code"
                                        error={errors.code}
                                        value={data.code}
                                        onChange={(e) =>
                                            setData('code', e.target.value)
                                        }
                                    />
                                </FieldGroup>
                            )}
                        </div>
                    </div>

                    <div className="card-footer flex items-center justify-between">
                        <button
                            type="button"
                            className="btn-link text-sm"
                            onClick={() => setUseRecoveryCode(!useRecoveryCode)}
                        >
                            {useRecoveryCode
                                ? 'Use authenticator code'
                                : 'Use recovery code'}
                        </button>
                        <LoadingButton
                            type="submit"
                            loading={processing}
                            className="btn-indigo"
                        >
                            Verify
                        </LoadingButton>
                    </div>
                </form>
            </div>
        </GuestLayout>
    );
}
