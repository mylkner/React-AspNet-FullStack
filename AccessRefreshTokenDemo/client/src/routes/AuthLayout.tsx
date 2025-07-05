import { Navigate, Outlet } from "react-router";

const AuthLayout = () => {
    const user: string | null = null; //placeholder
    return user == null ? <Navigate to="/login" /> : <Outlet />;
};

export default AuthLayout;
