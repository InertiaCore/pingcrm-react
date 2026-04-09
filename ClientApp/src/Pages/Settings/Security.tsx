import LoadingButton from '@/Components/Button/LoadingButton';
import FieldGroup from '@/Components/Form/FieldGroup';
import TextInput from '@/Components/Form/TextInput';
import MainLayout from '@/Layouts/MainLayout';
import { PageProps } from '@/types';
import { Head, Link, useForm, usePage } from '@inertiajs/react';
import React from 'react';

const Security = () => {
    const { auth } = usePage<PageProps>().props;

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
            </div>
        </div>
    );
};

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
                            address. Your email won't change until you click it.
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

Security.layout = (page: React.ReactNode) => <MainLayout children={page} />;

export default Security;
