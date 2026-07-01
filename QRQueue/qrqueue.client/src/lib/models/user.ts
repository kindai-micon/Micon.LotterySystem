export type SendAuthority = {
    name: string;
};

export type SendRole = {
    name: string;
    authorities: SendAuthority[];
};

export type SendUser = {
    userName: string;
    roles: SendRole[];
};
